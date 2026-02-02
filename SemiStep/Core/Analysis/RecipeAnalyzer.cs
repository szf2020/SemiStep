using Core.Entities;

using Shared.Reasons;

namespace Core.Analysis;

public sealed class RecipeAnalyzer(LoopParser loopParser, TimingCalculator timingCalculator)
{
	private const int MaxLoopDepth = 10;

	public RecipeResult Analyze(Recipe recipe)
	{
		if (recipe.Steps.Count == 0)
		{
			return RecipeResult.WithReasons(
				recipe,
				LoopStructure.Empty,
				TimingResult.Empty,
				[new EmptyRecipeWarning("Recipe is empty")]);
		}

		var loopParse = loopParser.Parse(recipe);

		var timing = timingCalculator.Calculate(recipe, loopParse.Loops);
		var enrichedLoops = timingCalculator.EnrichLoopsWithDuration(loopParse.Loops, timing.StepStartTimes);
		var loopStructure = LoopStructure.Create(enrichedLoops);

		var reasons = new List<AbstractReason>(loopParse.Reasons);

		if (loopParse.HasIntegrityIssues)
		{
			reasons.Add(new LoopIntegrityError("Loop structure has integrity issues (unmatched For/EndFor)"));
		}

		var maxDepth = enrichedLoops
			.Where(l => l.Status == LoopStatus.Valid)
			.Select(l => l.NestingDepth)
			.DefaultIfEmpty(0)
			.Max();

		if (maxDepth > MaxLoopDepth)
		{
			reasons.Add(new LoopNestingDepthError($"Maximum loop nesting depth ({maxDepth}) exceeded"));
		}

		return RecipeResult.WithReasons(recipe, loopStructure, timing, reasons);
	}
}

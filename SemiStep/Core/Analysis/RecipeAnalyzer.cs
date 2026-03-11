using Shared.Core;

namespace Core.Analysis;

internal sealed class RecipeAnalyzer(TimingCalculator timingCalculator, LoopParser loopParser)
{
	private const int MaxLoopDepth = 3;

	public RecipeSnapshot Analyze(Recipe recipe)
	{
		if (recipe.Steps.Count == 0)
		{
			return RecipeSnapshot.Create(
				recipe,
				TimeSpan.Zero,
				new Dictionary<int, TimeSpan>(),
				[],
				errors: [],
				warnings: ["Recipe is empty"]);
		}

		var loopParse = loopParser.Parse(recipe);
		var errors = new List<string>(loopParse.Errors);
		var warnings = new List<string>();

		var (stepStartTimes, totalDuration) = timingCalculator.Calculate(recipe, loopParse.Loops);

		var maxDepth = loopParse.Loops.Count > 0
			? loopParse.Loops.Max(l => l.Depth)
			: 0;

		if (maxDepth > MaxLoopDepth)
		{
			errors.Add($"Maximum loop nesting depth ({MaxLoopDepth}) exceeded: {maxDepth}");
		}

		return RecipeSnapshot.Create(
			recipe,
			totalDuration,
			stepStartTimes,
			loopParse.Loops,
			errors,
			warnings);
	}
}

using Core.Entities;

using Shared.Reasons;
using Shared.Registries;

namespace Core.Analysis;

public sealed class LoopParser(IActionRegistry actionRegistry)
{
	public LoopParseResult Parse(Recipe recipe)
	{
		var loops = new List<LoopInfo>();
		var warnings = new List<AbstractReason>();
		var stack = new Stack<ForFrame>();

		for (var i = 0; i < recipe.Steps.Count; i++)
		{
			var step = recipe.Steps[i];
			var actionId = GetActionId(step.ActionKey);

			if (actionId == null)
			{
				continue;
			}

			if (actionId == (int?)ServiceActionId.ForLoop)
			{
				var iterations = ExtractIterationCount(step);
				var depth = stack.Count + 1;
				stack.Push(new ForFrame(i, iterations, depth));
			}
			else if (actionId == (int?)ServiceActionId.EndForLoop)
			{
				if (stack.Count == 0)
				{
					var orphanLoop = new LoopInfo(
						StartIndex: i,
						EndIndex: i,
						NestingDepth: 0,
						IterationCount: 1,
						SingleIterationDuration: null,
						Status: LoopStatus.OrphanEnd);
					loops.Add(orphanLoop);
					warnings.Add(new UnclosedLoopWarning($"Unmatched EndFor at step {i}"));
				}
				else
				{
					var frame = stack.Pop();
					var validLoop = new LoopInfo(
						StartIndex: frame.StartIndex,
						EndIndex: i,
						NestingDepth: frame.NestingDepth,
						IterationCount: frame.IterationCount,
						SingleIterationDuration: null,
						Status: LoopStatus.Valid);
					loops.Add(validLoop);
				}
			}
		}

		while (stack.Count > 0)
		{
			var frame = stack.Pop();
			var incompleteLoop = new LoopInfo(
				StartIndex: frame.StartIndex,
				EndIndex: null,
				NestingDepth: frame.NestingDepth,
				IterationCount: frame.IterationCount,
				SingleIterationDuration: null,
				Status: LoopStatus.Incomplete);
			loops.Add(incompleteLoop);
			warnings.Add(new UnclosedLoopWarning($"Unclosed For loop starting at step {frame.StartIndex}"));
		}

		return new LoopParseResult(loops, warnings);
	}

	private int? GetActionId(int actionId)
	{
		if (!actionRegistry.ActionExists(actionId))
		{
			return null;
		}

		return actionId;
	}

	private static int ExtractIterationCount(Step step)
	{
		var iterationColumn = new ColumnId("iterations");
		if (!step.Properties.TryGetValue(iterationColumn, out var iterationProperty))
		{
			return 1;
		}

		return iterationProperty.Type switch
		{
			PropertyType.Int => iterationProperty.AsInt(),
			PropertyType.Float => (int)iterationProperty.AsFloat(),
			_ => 1
		};
	}

	private sealed record ForFrame(int StartIndex, int IterationCount, int NestingDepth);
}

public sealed record LoopParseResult(
	IReadOnlyList<LoopInfo> Loops,
	IReadOnlyList<AbstractReason> Reasons)
{
	public bool HasIntegrityIssues => Loops.Any(l => l.Status != LoopStatus.Valid);
}

namespace Shared.Core;

public sealed record RecipeSnapshot(
	Recipe Recipe,
	TimeSpan TotalDuration,
	IReadOnlyDictionary<int, TimeSpan> StepStartTimes,
	IReadOnlyList<LoopInfo> Loops,
	IReadOnlyDictionary<int, LoopInfo> LoopByStart,
	IReadOnlyDictionary<int, LoopInfo> LoopByEnd,
	IReadOnlyDictionary<int, IReadOnlyList<LoopInfo>> EnclosingLoops,
	IReadOnlyList<string> Errors,
	IReadOnlyList<string> Warnings)
{
	public static readonly RecipeSnapshot Empty = new(
		Recipe.Empty,
		TimeSpan.Zero,
		new Dictionary<int, TimeSpan>(),
		[],
		new Dictionary<int, LoopInfo>(),
		new Dictionary<int, LoopInfo>(),
		new Dictionary<int, IReadOnlyList<LoopInfo>>(),
		[],
		[]);
	public bool IsValid => Errors.Count == 0;

	public static RecipeSnapshot Create(
		Recipe recipe,
		TimeSpan totalDuration,
		IReadOnlyDictionary<int, TimeSpan> stepStartTimes,
		IReadOnlyList<LoopInfo> loops,
		IReadOnlyList<string> errors,
		IReadOnlyList<string> warnings)
	{
		var byStart = loops.ToDictionary(l => l.StartIndex, l => l);
		var byEnd = loops.ToDictionary(l => l.EndIndex, l => l);
		var enclosing = BuildEnclosingMap(loops);

		return new RecipeSnapshot(
			recipe,
			totalDuration,
			stepStartTimes,
			loops,
			byStart,
			byEnd,
			enclosing,
			errors,
			warnings);
	}

	private static Dictionary<int, IReadOnlyList<LoopInfo>> BuildEnclosingMap(IReadOnlyList<LoopInfo> loops)
	{
		var builder = new Dictionary<int, List<LoopInfo>>();

		foreach (var loop in loops)
		{
			for (var i = loop.StartIndex + 1; i < loop.EndIndex; i++)
			{
				if (!builder.TryGetValue(i, out var list))
				{
					list = [];
					builder[i] = list;
				}

				list.Add(loop);
			}
		}

		return builder.ToDictionary(
			kvp => kvp.Key, IReadOnlyList<LoopInfo> (kvp) => kvp.Value
				.OrderBy(l => l.Depth)
				.ToList()
				.AsReadOnly());
	}
}

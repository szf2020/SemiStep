using System.Collections.Immutable;

namespace Core.Entities;

public sealed record Recipe(ImmutableList<Step> Steps)
{
	public static readonly Recipe Empty = new([]);

	public int StepCount => Steps.Count;
}

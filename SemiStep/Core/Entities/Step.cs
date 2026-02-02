using System.Collections.Immutable;

namespace Core.Entities;

public sealed record Step(
	int ActionKey,
	ImmutableDictionary<ColumnId, PropertyValue> Properties);

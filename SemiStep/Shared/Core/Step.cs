using System.Collections.Immutable;

namespace Shared.Core;

public sealed record Step(
	int ActionKey,
	ImmutableDictionary<ColumnId, PropertyValue> Properties);

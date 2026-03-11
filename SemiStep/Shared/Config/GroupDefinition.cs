namespace Shared.Config;

public sealed record GroupDefinition(
	string GroupId,
	IReadOnlyDictionary<int, string> Items);

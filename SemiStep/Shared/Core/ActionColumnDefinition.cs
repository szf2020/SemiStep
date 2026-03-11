namespace Shared.Core;

public sealed record ActionColumnDefinition(
	string Key,
	string? GroupName,
	string PropertyTypeId,
	string? DefaultValue);

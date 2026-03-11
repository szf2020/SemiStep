namespace Shared.Core;

public sealed record PropertyDefinition(
	string PropertyTypeId,
	string SystemType,
	string FormatKind,
	string? Units,
	double? Min,
	double? Max,
	int? MaxLength);

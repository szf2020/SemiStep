namespace Shared.Entities;

public sealed record FormulaDefinition(
	string Expression,
	IReadOnlyList<string> RecalcOrder);

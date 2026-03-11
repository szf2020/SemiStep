namespace Core.Formulas;

internal sealed record FormulaDefinition(
	string Expression,
	IReadOnlyList<string> RecalcOrder);

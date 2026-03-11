namespace Core.Formulas;

internal sealed record FormulaResult(
	IReadOnlyDictionary<string, double>? ComputedValues,
	string? Error)
{
	public bool IsSuccess => Error is null;

	public static FormulaResult Success(IReadOnlyDictionary<string, double> values)
	{
		return new(values, null);
	}

	public static FormulaResult Failure(string error)
	{
		return new(null, error);
	}
}

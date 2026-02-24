using Shared.Reasons;

namespace Core.Formulas;

public sealed record FormulaResult(
	IReadOnlyDictionary<string, double>? ComputedValues,
	AbstractError? Error)
{
	public bool IsSuccess => Error is null;

	public static FormulaResult Success(IReadOnlyDictionary<string, double> values)
	{
		return new(values, null);
	}

	public static FormulaResult Failure(AbstractError error)
	{
		return new(null, error);
	}
}

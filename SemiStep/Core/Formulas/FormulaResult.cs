using Shared.Reasons;

namespace Core.Formulas;

public sealed record FormulaResult(
	IReadOnlyDictionary<string, double>? ComputedValues,
	AbstractError? Error)
{
	public bool IsSuccess => Error is null;

	public static FormulaResult Success(IReadOnlyDictionary<string, double> values)
		=> new(values, null);

	public static FormulaResult Failure(AbstractError error)
		=> new(null, error);
}

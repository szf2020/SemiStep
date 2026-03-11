using Shared.Core;

namespace Shared.Csv;

public sealed record CsvLoadResult(
	Recipe? Recipe,
	IReadOnlyList<string> Errors,
	IReadOnlyList<string> Warnings)
{
	public bool HasErrors => Errors.Count > 0;
	public bool IsSuccess => Recipe is not null && !HasErrors;

	public static CsvLoadResult Success(Recipe recipe)
	{
		return new CsvLoadResult(recipe, [], []);
	}

	public static CsvLoadResult Success(Recipe recipe, IReadOnlyList<string> warnings)
	{
		return new CsvLoadResult(recipe, [], warnings);
	}

	public static CsvLoadResult Failure(IReadOnlyList<string> errors)
	{
		return new CsvLoadResult(null, errors, []);
	}
}

using Core.Entities;

using Shared.Reasons;

namespace Domain.Models;

public sealed record CsvLoadResult(
	Recipe? Recipe,
	IReadOnlyList<AbstractReason> Reasons)
{
	public bool HasErrors => Reasons.OfType<AbstractError>().Any();
	public bool IsSuccess => Recipe is not null && !HasErrors;

	public static CsvLoadResult Success(Recipe recipe)
	{
		return new CsvLoadResult(recipe, []);
	}

	public static CsvLoadResult Success(Recipe recipe, IReadOnlyList<AbstractReason> warnings)
	{
		return new CsvLoadResult(recipe, warnings);
	}

	public static CsvLoadResult Failure(IReadOnlyList<AbstractReason> errors)
	{
		return new CsvLoadResult(null, errors);
	}
}

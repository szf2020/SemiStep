using FluentResults;

using TypesShared.Core;

namespace TypesShared.Domain;

public interface ICsvService
{
	Task<Result<Recipe>> LoadAsync(string filePath, CancellationToken cancellationToken = default);

	Task SaveAsync(Recipe recipe, string filePath, CancellationToken cancellationToken = default);
}

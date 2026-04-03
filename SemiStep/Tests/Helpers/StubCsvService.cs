using FluentResults;

using TypesShared.Core;
using TypesShared.Domain;

namespace Tests.Helpers;

public sealed class StubCsvService : ICsvService
{
	public Task<Result<Recipe>> LoadAsync(string filePath, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("StubCsvService does not support loading.");
	}

	public Task SaveAsync(Recipe recipe, string filePath, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("StubCsvService does not support saving.");
	}
}

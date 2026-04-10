using FluentResults;

using TypesShared.Core;
using TypesShared.Domain;

namespace Tests.Helpers;

public sealed class FailingCsvService : ICsvService
{
	public Task<Result<Recipe>> LoadAsync(string filePath)
	{
		return Task.FromResult(Result.Fail<Recipe>("FailingCsvService does not support loading."));
	}

	public Task SaveAsync(Recipe recipe, string filePath)
	{
		throw new IOException("Simulated disk write failure.");
	}
}

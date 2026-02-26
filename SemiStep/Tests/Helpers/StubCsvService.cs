using Core.Entities;

using Domain.Ports;

namespace Tests.Helpers;

public sealed class StubCsvService : ICsvService
{
	public Task<Recipe> LoadAsync(string filePath, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("StubRecipeRepository does not support loading.");
	}

	public Task SaveAsync(Recipe recipe, string filePath, CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException("StubRecipeRepository does not support saving.");
	}
}

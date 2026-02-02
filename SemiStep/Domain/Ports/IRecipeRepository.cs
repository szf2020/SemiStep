using Core.Entities;

namespace Domain.Ports;

public interface IRecipeRepository
{
	Task<Recipe> LoadAsync(string filePath, CancellationToken cancellationToken = default);

	Task SaveAsync(Recipe recipe, string filePath, CancellationToken cancellationToken = default);

	bool CanHandle(string filePath);
}

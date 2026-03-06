using Core.Entities;

using Domain.Models;

namespace Domain.Ports;

public interface ICsvService
{
	Task<CsvLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default);

	Task SaveAsync(Recipe recipe, string filePath, CancellationToken cancellationToken = default);
}

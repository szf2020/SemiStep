using Shared.Core;
using Shared.Csv;

namespace Shared.ServiceContracts;

public interface ICsvService
{
	Task<CsvLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default);

	Task SaveAsync(Recipe recipe, string filePath, CancellationToken cancellationToken = default);
}

using Csv.ClipboardService;

using FluentResults;

using Shared.Core;
using Shared.ServiceContracts;

namespace Csv.Facade;

internal sealed class CsvClipboard(CsvClipboardSerializer clipboardSerializer) : ICsvClipboardService
{
	public string SerializeSteps(IReadOnlyList<Step> steps)
	{
		return clipboardSerializer.SerializeSteps(steps);
	}

	public Result<IReadOnlyList<Step>> DeserializeSteps(string csvBody)
	{
		return clipboardSerializer.DeserializeSteps(csvBody);
	}
}

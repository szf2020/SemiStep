using FluentResults;

using Shared.Core;

namespace Shared.ServiceContracts;

public interface ICsvClipboardService
{
	string SerializeSteps(IReadOnlyList<Step> steps);

	Result<IReadOnlyList<Step>> DeserializeSteps(string csvBody);
}

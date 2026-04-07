using TypesShared.Core;
using TypesShared.Plc;

namespace TypesShared.Domain;

public interface IPlcSyncService
{
	void NotifyRecipeChanged(Recipe recipe, bool isValid);

	void Reset();

	PlcSyncStatus Status { get; }

	string? LastError { get; }

	DateTimeOffset? LastSyncTime { get; }

	event Action<PlcSyncStatus>? StatusChanged;

	event Action<string?>? ErrorChanged;
}

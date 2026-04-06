using FluentResults;

using TypesShared.Core;
using TypesShared.Plc;

namespace TypesShared.Domain;

public interface IS7Service : IAsyncDisposable
{
	bool IsConnected { get; }
	bool IsRecipeActive { get; }
	IObservable<PlcExecutionInfo> ExecutionState { get; }
	event Action<PlcConnectionState>? StateChanged;
	Task ConnectAsync(PlcConnectionSettings settings, CancellationToken ct = default);
	Task DisconnectAsync(CancellationToken ct = default);
	Task<Result<PlcManagingAreaState>> ReadManagingAreaAsync(CancellationToken ct = default);
	Task<Result<Recipe>> ReadRecipeFromPlcAsync(CancellationToken ct = default);
}

using Shared.Plc;

namespace Shared.ServiceContracts;

public interface IS7ConnectionService : IAsyncDisposable
{
	bool IsConnected { get; }
	event Action<PlcConnectionState>? StateChanged;
	new ValueTask DisposeAsync();
	Task ConnectAsync(PlcConnectionSettings settings, CancellationToken ct = default);
	Task DisconnectAsync(CancellationToken ct = default);
}

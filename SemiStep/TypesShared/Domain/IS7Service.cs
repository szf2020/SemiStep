using TypesShared.Plc;

namespace TypesShared.Domain;

public interface IS7Service : IAsyncDisposable
{
	bool IsConnected { get; }
	event Action<PlcConnectionState>? StateChanged;
	new ValueTask DisposeAsync();
	Task ConnectAsync(PlcConnectionSettings settings, CancellationToken ct = default);
	Task DisconnectAsync(CancellationToken ct = default);
}

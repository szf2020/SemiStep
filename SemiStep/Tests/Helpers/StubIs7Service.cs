using TypesShared.Domain;
using TypesShared.Plc;

namespace Tests.Helpers;

public sealed class StubIs7Service : IS7Service
{
	public bool IsConnected => false;

	public event Action<PlcConnectionState>? StateChanged;

	public Task ConnectAsync(PlcConnectionSettings settings, CancellationToken ct = default)
	{
		return Task.CompletedTask;
	}

	public Task DisconnectAsync(CancellationToken ct = default)
	{
		return Task.CompletedTask;
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}

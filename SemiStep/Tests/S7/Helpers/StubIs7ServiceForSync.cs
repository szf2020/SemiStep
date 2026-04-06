using System.Reactive.Linq;

using FluentResults;

using TypesShared.Core;
using TypesShared.Domain;
using TypesShared.Plc;

namespace Tests.S7.Helpers;

internal sealed class StubIs7ServiceForSync : IS7Service
{
	private bool _connected;

	public StubIs7ServiceForSync(bool connected)
	{
		_connected = connected;
	}

	public bool IsConnected => _connected;

	public bool IsRecipeActive => false;

	public IObservable<PlcExecutionInfo> ExecutionState =>
		Observable.Empty<PlcExecutionInfo>();

	public event Action<PlcConnectionState>? StateChanged;

	public void SetConnected(bool connected)
	{
		_connected = connected;
	}

	public Task ConnectAsync(PlcConnectionSettings settings, CancellationToken ct = default)
	{
		return Task.CompletedTask;
	}

	public Task DisconnectAsync(CancellationToken ct = default)
	{
		return Task.CompletedTask;
	}

	public Task<Result<PlcManagingAreaState>> ReadManagingAreaAsync(CancellationToken ct = default)
	{
		return Task.FromResult(Result.Fail<PlcManagingAreaState>("Not connected"));
	}

	public Task<Result<Recipe>> ReadRecipeFromPlcAsync(CancellationToken ct = default)
	{
		return Task.FromResult(Result.Fail<Recipe>("Not connected"));
	}

	public ValueTask DisposeAsync()
	{
		return ValueTask.CompletedTask;
	}
}

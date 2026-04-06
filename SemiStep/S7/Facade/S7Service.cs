using FluentResults;

using S7.Sync;

using Serilog;

using TypesShared.Core;
using TypesShared.Domain;
using TypesShared.Plc;

namespace S7.Facade;

internal sealed class S7Service(
	IS7Driver transport,
	PlcExecutionMonitor executionMonitor,
	PlcTransactionExecutor transactionExecutor,
	PlcConfiguration plcConfiguration)
	: IS7Service
{
	private readonly Lock _stateLock = new();
	private PlcConnectionState _state = PlcConnectionState.Disconnected;
	private bool _autoReconnectEnabled;
	private CancellationTokenSource? _reconnectCts;
	private Task? _reconnectTask;
	private CancellationTokenSource? _keepAliveCts;
	private Task? _keepAliveTask;
	private PlcConnectionSettings? _settings;

	public PlcConnectionState State
	{
		get
		{
			lock (_stateLock)
			{
				return _state;
			}
		}
		private set
		{
			lock (_stateLock)
			{
				if (_state == value)
				{
					return;
				}
				_state = value;
			}
			StateChanged?.Invoke(value);
		}
	}

	public async ValueTask DisposeAsync()
	{
		await DisconnectAsync();
		executionMonitor.Dispose();
		await transport.DisposeAsync();
	}

	public bool IsConnected => State == PlcConnectionState.Connected;

	public bool IsRecipeActive => executionMonitor.LastKnown.RecipeActive;

	public IObservable<PlcExecutionInfo> ExecutionState => executionMonitor.State;

	public event Action<PlcConnectionState>? StateChanged;

	public async Task ConnectAsync(PlcConnectionSettings settings, CancellationToken ct = default)
	{
		_settings = settings;
		_autoReconnectEnabled = true;

		await ConnectInternalAsync(ct);
	}

	public async Task DisconnectAsync(CancellationToken ct = default)
	{
		_autoReconnectEnabled = false;
		var reconnectTaskToAwait = StopReconnectLoop();
		if (reconnectTaskToAwait is not null)
		{
			try
			{
				await reconnectTaskToAwait;
			}
			catch (OperationCanceledException)
			{
			}
		}

		var keepAliveTaskToAwait = StopKeepAlive();
		if (keepAliveTaskToAwait is not null)
		{
			try
			{
				await keepAliveTaskToAwait;
			}
			catch (OperationCanceledException)
			{
			}
		}

		await executionMonitor.StopAsync();

		if (transport.IsConnected)
		{
			await transport.DisconnectAsync(ct);
		}

		State = PlcConnectionState.Disconnected;
		Log.Information("Disconnected from PLC");
	}

	public async Task<Result<PlcManagingAreaState>> ReadManagingAreaAsync(CancellationToken ct = default)
	{
		try
		{
			var state = await transactionExecutor.ReadManagingAreaAsync(ct);
			return Result.Ok(new PlcManagingAreaState(state.Committed, state.RecipeLines));
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to read managing area from PLC");
			return Result.Fail(ex.Message);
		}
	}

	public async Task<Result<Recipe>> ReadRecipeFromPlcAsync(CancellationToken ct = default)
	{
		try
		{
			return await transactionExecutor.ReadRecipeFromPlcAsync(ct);
		}
		catch (Exception ex)
		{
			Log.Warning(ex, "Failed to read recipe from PLC");
			return Result.Fail(ex.Message);
		}
	}

	internal void OnConnectionLost()
	{
		lock (_stateLock)
		{
			if (_state != PlcConnectionState.Connected)
			{
				return;
			}

			_state = PlcConnectionState.Disconnected;
		}

		StateChanged?.Invoke(PlcConnectionState.Disconnected);

		_ = StopKeepAlive();
		executionMonitor.Stop();
		Log.Warning("PLC connection lost");

		if (_autoReconnectEnabled && _settings is not null)
		{
			StartReconnectLoop();
		}
	}

	private async Task ConnectInternalAsync(CancellationToken ct)
	{
		if (_settings is null)
		{
			throw new InvalidOperationException("Connection settings not configured");
		}

		State = PlcConnectionState.Connecting;
		Log.Information("Connecting to PLC at {IpAddress}...", _settings.IpAddress);

		try
		{
			await transport.ConnectAsync(_settings, ct);
			State = PlcConnectionState.Connected;
			Log.Information("Connected to PLC at {IpAddress}", _settings.IpAddress);

			executionMonitor.Start();
			StartKeepAlive();
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			State = PlcConnectionState.Disconnected;
			Log.Error(ex, "Failed to connect to PLC at {IpAddress}", _settings.IpAddress);

			throw;
		}
	}

	private void StartKeepAlive()
	{
		lock (_stateLock)
		{
			if (_keepAliveTask is not null)
			{
				return;
			}

			_keepAliveCts = new CancellationTokenSource();
			_keepAliveTask = KeepAliveLoopAsync(_keepAliveCts.Token);
		}
	}

	private Task? StopKeepAlive()
	{
		lock (_stateLock)
		{
			_keepAliveCts?.Cancel();
			_keepAliveCts?.Dispose();
			_keepAliveCts = null;

			var taskToAwait = _keepAliveTask;
			_keepAliveTask = null;

			return taskToAwait;
		}
	}

	private async Task KeepAliveLoopAsync(CancellationToken ct)
	{
		var dbNumber = plcConfiguration.Layout.ManagingDb.DbNumber;
		var intervalMs = plcConfiguration.ProtocolSettings.KeepAliveIntervalMs;

		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(intervalMs, ct);
				await transport.ReadBytesAsync(dbNumber, 0, 1, ct);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Keep-alive probe failed, connection assumed lost");
				OnConnectionLost();

				return;
			}
		}
	}

	private void StartReconnectLoop()
	{
		lock (_stateLock)
		{
			if (_reconnectTask is not null)
			{
				return;
			}

			_reconnectCts = new CancellationTokenSource();
			_reconnectTask = ReconnectLoopAsync(_reconnectCts.Token);
		}
	}

	private Task? StopReconnectLoop()
	{
		lock (_stateLock)
		{
			_reconnectCts?.Cancel();
			_reconnectCts?.Dispose();
			_reconnectCts = null;

			var taskToAwait = _reconnectTask;
			_reconnectTask = null;

			return taskToAwait;
		}
	}

	private async Task ReconnectLoopAsync(CancellationToken ct)
	{
		var delay = TimeSpan.FromSeconds(1);
		var maxDelay = TimeSpan.FromSeconds(30);

		while (!ct.IsCancellationRequested && _autoReconnectEnabled)
		{
			try
			{
				await Task.Delay(delay, ct);
				await ConnectInternalAsync(ct);

				lock (_stateLock)
				{
					_reconnectTask = null;
				}

				return;
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Reconnection attempt failed, retrying in {Delay}s", delay.TotalSeconds);
				delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
			}
		}
	}
}

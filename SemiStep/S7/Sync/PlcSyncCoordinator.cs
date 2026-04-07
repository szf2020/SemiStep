using System.Linq;

using FluentResults;

using S7.Protocol;

using Serilog;

using TypesShared.Core;
using TypesShared.Domain;
using TypesShared.Plc;

namespace S7.Sync;

internal sealed class PlcSyncCoordinator(
	PlcTransactionExecutor transactionExecutor,
	IS7Service connectionService)
	: IPlcSyncService, IDisposable
{
	private const int DebounceDelayMs = 1000;
	private readonly Lock _lock = new();

	private CancellationTokenSource? _debounceCts;
	private bool _disposed;
	private string? _lastError;
	private DateTimeOffset? _lastSyncTime;
	private Recipe? _pendingSnapshot;
	private PlcSyncStatus _status = PlcSyncStatus.Idle;
	private Task? _syncTask;

	public PlcSyncStatus Status
	{
		get
		{
			lock (_lock)
			{
				return _status;
			}
		}
		private set
		{
			lock (_lock)
			{
				if (_status == value)
				{
					return;
				}
				_status = value;
			}
			// value is captured before leaving the lock. StatusChanged is raised outside the
			// lock intentionally: subscribers must not re-enter the lock on the same thread.
			StatusChanged?.Invoke(value);
		}
	}

	public string? LastError
	{
		get
		{
			lock (_lock)
			{
				return _lastError;
			}
		}
		private set
		{
			lock (_lock)
			{
				_lastError = value;
			}
			ErrorChanged?.Invoke(value);
		}
	}

	public DateTimeOffset? LastSyncTime
	{
		get
		{
			lock (_lock)
			{
				return _lastSyncTime;
			}
		}
		private set
		{
			lock (_lock)
			{
				_lastSyncTime = value;
			}
		}
	}

	public event Action<PlcSyncStatus>? StatusChanged;
	public event Action<string?>? ErrorChanged;

	public void NotifyRecipeChanged(Recipe recipe, bool isValid)
	{
		if (_disposed)
		{
			return;
		}

		if (!isValid)
		{
			lock (_lock)
			{
				_pendingSnapshot = null;
			}
			Status = PlcSyncStatus.OutOfSync;
			return;
		}

		OnRecipeChanged(recipe);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_debounceCts?.Cancel();
		_debounceCts?.Dispose();
	}

	public void Reset()
	{
		lock (_lock)
		{
			_debounceCts?.Cancel();
			_debounceCts?.Dispose();
			_debounceCts = null;
			_pendingSnapshot = null;
		}

		LastError = null;
		Status = PlcSyncStatus.Disconnected;
	}

	public async Task WaitForPendingSyncAsync(CancellationToken ct = default)
	{
		Task? taskToWait;
		lock (_lock)
		{
			taskToWait = _syncTask;
		}

		if (taskToWait is not null)
		{
			try
			{
				await taskToWait.WaitAsync(ct);
			}
			catch (OperationCanceledException)
			{
			}
		}
	}

	private void OnRecipeChanged(Recipe recipe)
	{
		if (_disposed)
		{
			return;
		}

		lock (_lock)
		{
			_pendingSnapshot = recipe;

			if (_syncTask is not null && !_syncTask.IsCompleted)
			{
				Log.Debug("Sync in progress, queueing new snapshot");

				return;
			}

			StartDebounce();
		}
	}

	private void StartDebounce()
	{
		_debounceCts?.Cancel();
		_debounceCts?.Dispose();
		_debounceCts = new CancellationTokenSource();

		var ct = _debounceCts.Token;
		_syncTask = Task.Run(async () =>
		{
			try
			{
				await Task.Delay(DebounceDelayMs, ct);
				await ExecuteSyncAsync(ct);
			}
			catch (OperationCanceledException)
			{
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Unhandled exception in sync task");
				Status = PlcSyncStatus.Failed;
			}
		}, ct);
	}

	private async Task ExecuteSyncAsync(CancellationToken ct)
	{
		Recipe? snapshotToSync;
		lock (_lock)
		{
			snapshotToSync = _pendingSnapshot;
			_pendingSnapshot = null;
		}

		if (snapshotToSync is null)
		{
			return;
		}

		if (!connectionService.IsConnected)
		{
			Log.Debug("Skipping sync: not connected to PLC");
			Status = PlcSyncStatus.Disconnected;

			return;
		}

		var activeResult = await transactionExecutor.IsRecipeActiveAsync(ct);
		if (activeResult.IsFailed)
		{
			var isDisconnected = activeResult.Errors.OfType<NotConnectedError>().Any();
			Status = PlcSyncStatus.Failed;
			LastError = isDisconnected
				? "Not connected to PLC"
				: activeResult.Errors[0].Message;

			if (isDisconnected)
			{
				Log.Warning("Sync blocked: not connected to PLC");
			}

			return;
		}

		if (activeResult.Value)
		{
			Status = PlcSyncStatus.Failed;
			LastError = "Recipe is being executed on PLC";
			Log.Warning("Sync blocked: recipe is being executed on PLC");

			return;
		}

		Status = PlcSyncStatus.Syncing;
		LastError = null;

		var writeResult = await transactionExecutor.WriteRecipeWithRetryAsync(snapshotToSync, ct);
		if (writeResult.IsFailed)
		{
			Status = PlcSyncStatus.Failed;
			LastError = writeResult.Errors[0].Message;
			if (!writeResult.Errors.OfType<NotConnectedError>().Any())
			{
				Log.Error("Sync failed: {Message}", writeResult.Errors[0].Message);
			}

			return;
		}

		LastSyncTime = DateTimeOffset.UtcNow;
		Status = PlcSyncStatus.Synced;
		LastError = null;

		lock (_lock)
		{
			if (_pendingSnapshot is not null && !_disposed)
			{
				Log.Debug("Changes occurred during sync, starting new debounce");
				StartDebounce();
			}
		}
	}
}

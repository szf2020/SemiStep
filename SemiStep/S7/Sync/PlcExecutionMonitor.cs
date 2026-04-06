using System.Reactive.Subjects;

using S7.Protocol;

using Serilog;

using TypesShared.Plc;

namespace S7.Sync;

internal sealed class PlcExecutionMonitor(
	PlcTransactionExecutor transactionExecutor,
	PlcProtocolSettings protocolSettings,
	Action onConnectionLost)
	: IDisposable
{
	private readonly Subject<PlcExecutionInfo> _subject = new();

	private volatile PlcExecutionInfo _lastKnown = PlcExecutionInfo.Empty;
	private CancellationTokenSource? _pollCts;
	private Task? _pollTask;

	public IObservable<PlcExecutionInfo> State => _subject;

	public PlcExecutionInfo LastKnown => _lastKnown;

	public void Start(CancellationToken externalCancellationToken = default)
	{
		Stop();

		_pollCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
		_pollTask = PollLoopAsync(_pollCts.Token);
	}

	public void Stop()
	{
		_pollCts?.Cancel();
		_pollCts?.Dispose();
		_pollCts = null;

		var taskToWait = _pollTask;
		_pollTask = null;

		// Wait for the poll loop to terminate before nulling the task, so that a
		// subsequent Start() cannot start a second concurrent loop.
		taskToWait?.Wait(TimeSpan.FromSeconds(5));

		PublishAndTrack(PlcExecutionInfo.Empty);
	}

	public async Task StopAsync()
	{
		_pollCts?.Cancel();
		_pollCts?.Dispose();
		_pollCts = null;

		var taskToWait = _pollTask;
		_pollTask = null;

		if (taskToWait is not null)
		{
			try
			{
				await taskToWait.WaitAsync(TimeSpan.FromSeconds(5));
			}
			catch (TimeoutException)
			{
				Log.Warning("Execution monitor poll loop did not stop within 5 seconds");
			}
		}

		PublishAndTrack(PlcExecutionInfo.Empty);
	}

	public void Dispose()
	{
		Stop();
		_subject.OnCompleted();
		_subject.Dispose();
	}

	private void PublishAndTrack(PlcExecutionInfo info)
	{
		_lastKnown = info;
		_subject.OnNext(info);
	}

	private async Task PollLoopAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			try
			{
				await Task.Delay(protocolSettings.PollingIntervalMs, ct);

				var state = await transactionExecutor.ReadExecutionStateAsync(ct);

				var info = new PlcExecutionInfo(
					RecipeActive: state.RecipeActive,
					ActualLine: state.ActualLine,
					StepCurrentTime: state.StepCurrentTime,
					ForLoopCount1: state.ForLoopCount1,
					ForLoopCount2: state.ForLoopCount2,
					ForLoopCount3: state.ForLoopCount3);

				PublishAndTrack(info);
			}
			catch (OperationCanceledException)
			{
				return;
			}
			catch (PlcNotConnectedException)
			{
				Log.Debug("Execution monitor stopping: PLC not connected");

				if (!(_pollCts?.IsCancellationRequested ?? true))
				{
					onConnectionLost();
				}

				return;
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Unexpected error in execution monitor poll loop");
			}
		}
	}
}

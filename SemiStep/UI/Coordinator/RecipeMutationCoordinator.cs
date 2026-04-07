using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;

using Avalonia.Threading;

using Domain.Facade;

using FluentResults;

using TypesShared.Config;
using TypesShared.Core;
using TypesShared.Domain;
using TypesShared.Plc;

using UI.MessageService;

namespace UI.Coordinator;

public sealed class RecipeMutationCoordinator(
	DomainFacade domainFacade,
	AppConfiguration appConfiguration,
	RecipeQueryService queryService,
	MessagePanelViewModel messagePanel,
	IPlcSyncService syncService) : IDisposable
{
	private readonly Subject<MutationSignal> _stateChanged = new();
	private readonly Subject<(Recipe Local, Recipe Plc)> _plcRecipeConflictDetected = new();
	private readonly Subject<Unit> _connectionStateChanged = new();
	private readonly Lock _subjectLock = new();
	private Action<string?>? _syncErrorChangedRelay;
	private bool _initialized;

	public IObservable<MutationSignal> StateChanged => _stateChanged;
	public IObservable<(Recipe Local, Recipe Plc)> PlcRecipeConflictDetected => _plcRecipeConflictDetected;
	public IObservable<Unit> ConnectionStateChanged => _connectionStateChanged;

	public int? SuggestedSelection { get; private set; }

	public Recipe CurrentRecipe => queryService.CurrentRecipe;

	public RecipeSnapshot Snapshot => queryService.Snapshot;

	public bool IsDirty => queryService.IsDirty;
	public bool CanUndo => queryService.CanUndo;
	public bool CanRedo => queryService.CanRedo;
	public bool IsConnected => queryService.IsConnected;

	public IObservable<PlcExecutionInfo> ExecutionState => queryService.ExecutionState;
	public bool IsRecipeActive => queryService.IsRecipeActive;
	public bool IsSyncEnabled => queryService.IsSyncEnabled;

	public RecipeQueryService QueryService => queryService;

	public RecipeMutationCoordinator Initialize()
	{
		if (_initialized)
		{
			throw new InvalidOperationException("RecipeMutationCoordinator has already been initialized.");
		}

		_initialized = true;

		domainFacade.PlcRecipeConflictDetected += OnPlcRecipeConflictDetected;
		domainFacade.ConnectionStateChanged += OnConnectionStateChanged;
		syncService.StatusChanged += OnSyncStatusChanged;

		_syncErrorChangedRelay = _ => NotifyConnectionStateChanged();
		syncService.ErrorChanged += _syncErrorChangedRelay;

		return this;
	}

	public void Dispose()
	{
		domainFacade.PlcRecipeConflictDetected -= OnPlcRecipeConflictDetected;
		domainFacade.ConnectionStateChanged -= OnConnectionStateChanged;
		syncService.StatusChanged -= OnSyncStatusChanged;

		if (_syncErrorChangedRelay is not null)
		{
			syncService.ErrorChanged -= _syncErrorChangedRelay;
		}

		_stateChanged.Dispose();
		_plcRecipeConflictDetected.Dispose();
		lock (_subjectLock)
		{
			_connectionStateChanged.Dispose();
		}
	}

	public int? ConsumeSuggestedSelection()
	{
		var value = SuggestedSelection;
		SuggestedSelection = null;

		return value;
	}

	public async Task<Result> EnableSync()
	{
		var result = await domainFacade.EnableSync(appConfiguration.PlcConfiguration);

		if (result.IsFailed)
		{
			var errorMessage = string.Join("; ", result.Errors.Select(e => e.Message));
			Dispatcher.UIThread.Post(() => messagePanel.AddError($"Failed to enable sync: {errorMessage}", "PLC"));
		}

		return result;
	}

	public async Task DisableSync()
	{
		await domainFacade.DisableSync();
	}

	public async Task<Result> LoadRecipeFromPlcAsync()
	{
		var result = await domainFacade.LoadRecipeFromPlcAsync();

		if (!result.IsFailed)
		{
			messagePanel.Clear();
		}

		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return result;
		}

		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());

		return result;
	}

	public void ResolveConflict(bool keepLocal)
	{
		domainFacade.ResolveConflict(keepLocal);
	}

	public void AppendStep(int actionId)
	{
		var result = domainFacade.AppendStep(actionId);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = CurrentRecipe.StepCount - 1;
		_stateChanged.OnNext(new MutationSignal.StepAppended(CurrentRecipe.StepCount - 1));
	}

	public void InsertStep(int index, int actionId)
	{
		var result = domainFacade.InsertStep(index, actionId);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = index;
		_stateChanged.OnNext(new MutationSignal.StepsInserted(index, 1));
	}

	public void RemoveStep(int index)
	{
		var result = domainFacade.RemoveStep(index);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = CurrentRecipe.StepCount > 0
			? Math.Min(index, CurrentRecipe.StepCount - 1)
			: null;
		_stateChanged.OnNext(new MutationSignal.StepRemoved(index));
	}

	public void RemoveSteps(IReadOnlyList<int> indices)
	{
		var result = domainFacade.RemoveSteps(indices);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = CurrentRecipe.StepCount > 0
			? Math.Min(indices.Min(), CurrentRecipe.StepCount - 1)
			: null;
		_stateChanged.OnNext(new MutationSignal.StepsRemoved([.. indices]));
	}

	public void InsertSteps(int startIndex, IReadOnlyList<Step> steps)
	{
		var result = domainFacade.InsertSteps(startIndex, steps);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = startIndex;
		_stateChanged.OnNext(new MutationSignal.StepsInserted(startIndex, steps.Count));
	}

	public void ChangeStepAction(int stepIndex, int newActionId)
	{
		var result = domainFacade.ChangeStepAction(stepIndex, newActionId);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = stepIndex;
		_stateChanged.OnNext(new MutationSignal.StepActionChanged(stepIndex));
	}

	public void UpdateStepProperty(int stepIndex, string columnKey, string value)
	{
		var result = domainFacade.UpdateStepProperty(stepIndex, columnKey, value);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		_stateChanged.OnNext(new MutationSignal.PropertyUpdated(stepIndex));
	}

	public void Undo()
	{
		var result = domainFacade.Undo();
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());
	}

	public void Redo()
	{
		var result = domainFacade.Redo();
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());
	}

	public void NewRecipe()
	{
		domainFacade.SetNewRecipe();
		messagePanel.Clear();
		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());
	}

	public async Task<Result> LoadRecipeAsync(string filePath)
	{
		var result = await domainFacade.LoadRecipeAsync(filePath);

		if (!result.IsFailed)
		{
			messagePanel.Clear();
		}

		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return result;
		}

		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());

		return result;
	}

	public async Task SaveRecipeAsync(string filePath)
	{
		await domainFacade.SaveRecipeAsync(filePath);
		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.MetadataChanged());
	}

	private void OnConnectionStateChanged(PlcConnectionState state)
	{
		NotifyConnectionStateChanged();

		// Disconnection events are suppressed when sync is not user-enabled, as they are
		// expected during normal startup and do not represent a loss of user-initiated sync.
		Dispatcher.UIThread.Post(() =>
		{
			var ipAddress = appConfiguration.PlcConfiguration.Connection.IpAddress;

			switch (state)
			{
				case PlcConnectionState.Connected:
					messagePanel.AddInfo($"Connected to PLC ({ipAddress})", "PLC");
					break;
				case PlcConnectionState.Disconnected when queryService.IsSyncEnabled:
					messagePanel.AddError("PLC connection lost", "PLC");
					break;
			}
		});
	}

	private void OnSyncStatusChanged(PlcSyncStatus status)
	{
		NotifyConnectionStateChanged();

		var lastError = syncService.LastError;
		Dispatcher.UIThread.Post(() =>
		{
			switch (status)
			{
				case PlcSyncStatus.Synced:
					messagePanel.AddInfo("Recipe synced to PLC", "PLC");
					break;
				case PlcSyncStatus.Failed:
					messagePanel.AddError(lastError ?? "Sync failed", "PLC");
					break;
			}
		});
	}

	private void OnPlcRecipeConflictDetected(Recipe local, Recipe plc)
	{
		Dispatcher.UIThread.Post(() => _plcRecipeConflictDetected.OnNext((local, plc)));
	}

	private void RefreshMessagePanel(Result mutationResult)
	{
		messagePanel.RefreshReasons(mutationResult.Reasons);
	}

	private void NotifyConnectionStateChanged()
	{
		lock (_subjectLock)
		{
			_connectionStateChanged.OnNext(Unit.Default);
		}
	}
}

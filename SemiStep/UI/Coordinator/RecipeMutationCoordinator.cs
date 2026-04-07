using System.Reactive;
using System.Reactive.Subjects;

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
	private Action? _connectionStateChangedRelay;
	private Action<PlcSyncStatus>? _statusChangedRelay;
	private Action<string?>? _errorChangedRelay;

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
		domainFacade.PlcRecipeConflictDetected += OnPlcRecipeConflictDetected;

		_connectionStateChangedRelay = () => _connectionStateChanged.OnNext(Unit.Default);
		domainFacade.ConnectionStateChanged += _connectionStateChangedRelay;

		_statusChangedRelay = _ => _connectionStateChanged.OnNext(Unit.Default);
		syncService.StatusChanged += _statusChangedRelay;

		_errorChangedRelay = _ => _connectionStateChanged.OnNext(Unit.Default);
		syncService.ErrorChanged += _errorChangedRelay;

		return this;
	}

	public void Dispose()
	{
		domainFacade.PlcRecipeConflictDetected -= OnPlcRecipeConflictDetected;

		if (_connectionStateChangedRelay is not null)
		{
			domainFacade.ConnectionStateChanged -= _connectionStateChangedRelay;
		}

		if (_statusChangedRelay is not null)
		{
			syncService.StatusChanged -= _statusChangedRelay;
		}

		if (_errorChangedRelay is not null)
		{
			syncService.ErrorChanged -= _errorChangedRelay;
		}

		_stateChanged.Dispose();
		_plcRecipeConflictDetected.Dispose();
		_connectionStateChanged.Dispose();
	}

	public int? ConsumeSuggestedSelection()
	{
		var value = SuggestedSelection;
		SuggestedSelection = null;

		return value;
	}

	public async Task<Result> EnableSync()
	{
		return await domainFacade.EnableSync(appConfiguration.PlcConfiguration);
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

	private void OnPlcRecipeConflictDetected(Recipe local, Recipe plc)
	{
		_plcRecipeConflictDetected.OnNext((local, plc));
	}

	private void RefreshMessagePanel(Result mutationResult)
	{
		messagePanel.RefreshReasons(mutationResult.Reasons);
	}
}

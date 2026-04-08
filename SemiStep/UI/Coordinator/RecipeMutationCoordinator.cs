using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Domain.Facade;

using FluentResults;

using ReactiveUI;

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
	MessagePanelViewModel messagePanel) : IDisposable
{
	private readonly Subject<MutationSignal> _stateChanged = new();
	private readonly Subject<(Recipe Local, Recipe Plc)> _plcRecipeConflictDetected = new();
	private readonly Subject<Result<PlcSessionSnapshot>> _plcStateChanged = new();
	private Result _lastRecipeResult = Result.Ok();
	private Result<PlcSessionSnapshot> _lastPlcState = PlcSessionSnapshot.InitialState;
	private IDisposable? _plcStateSubscription;
	private bool _initialized;
	private bool _disposed;

	public IObservable<MutationSignal> StateChanged => _stateChanged;
	public IObservable<(Recipe Local, Recipe Plc)> PlcRecipeConflictDetected => _plcRecipeConflictDetected;
	public IObservable<Result<PlcSessionSnapshot>> PlcStateChanged => _plcStateChanged;

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

		_plcStateSubscription = domainFacade.PlcState
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(OnPlcStateChanged);

		return this;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		domainFacade.PlcRecipeConflictDetected -= OnPlcRecipeConflictDetected;

		_plcStateSubscription?.Dispose();

		_stateChanged.Dispose();
		_plcRecipeConflictDetected.Dispose();
		_plcStateChanged.Dispose();
	}

	public int? ConsumeSuggestedSelection()
	{
		var value = SuggestedSelection;
		SuggestedSelection = null;

		return value;
	}

	public Task<Result> EnableSync()
	{
		return domainFacade.EnableSync(appConfiguration.PlcConfiguration);
	}

	public async Task DisableSync()
	{
		await domainFacade.DisableSync();
	}

	public async Task<Result> LoadRecipeFromPlcAsync()
	{
		var result = await domainFacade.LoadRecipeFromPlcAsync();

		_lastRecipeResult = Result.Ok();

		if (result.IsFailed)
		{
			_lastRecipeResult = result;
		}

		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		_stateChanged.OnNext(new MutationSignal.PropertyUpdated(stepIndex));
	}

	public void Undo()
	{
		var result = domainFacade.Undo();
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = result;
		RebuildMessagePanel();

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
		_lastRecipeResult = Result.Ok();
		RebuildMessagePanel();
		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());
	}

	public async Task<Result> LoadRecipeAsync(string filePath)
	{
		var result = await domainFacade.LoadRecipeAsync(filePath);

		_lastRecipeResult = Result.Ok();

		if (result.IsFailed)
		{
			_lastRecipeResult = result;
		}

		RebuildMessagePanel();

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

	private void OnPlcStateChanged(Result<PlcSessionSnapshot> result)
	{
		_lastPlcState = result;
		_plcStateChanged.OnNext(result);
		RebuildMessagePanel();
	}

	private void OnPlcRecipeConflictDetected(Recipe local, Recipe plc)
	{
		Avalonia.Threading.Dispatcher.UIThread.Post(() => _plcRecipeConflictDetected.OnNext((local, plc)));
	}

	private void RebuildMessagePanel()
	{
		var combinedReasons = _lastRecipeResult.Reasons.Concat(_lastPlcState.Reasons);
		messagePanel.RefreshReasons(combinedReasons);
	}
}

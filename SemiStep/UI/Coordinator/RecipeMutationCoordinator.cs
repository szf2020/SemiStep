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

public sealed class RecipeMutationCoordinator : IDisposable
{
	private readonly DomainFacade _domainFacade;
	private readonly AppConfiguration _appConfiguration;
	private readonly RecipeQueryService _queryService;
	private readonly MessagePanelViewModel _messagePanel;
	private readonly RecipeStepCoordinator _stepCoordinator;
	private readonly Subject<MutationSignal> _stateChanged = new();
	private readonly Subject<(Recipe Local, Recipe Plc)> _plcRecipeConflictDetected = new();
	private readonly Subject<Result<PlcSessionSnapshot>> _plcStateChanged = new();
	private Result _lastRecipeResult = Result.Ok();
	private Result<PlcSessionSnapshot> _lastPlcState = PlcSessionSnapshot.InitialState;
	private IDisposable? _plcStateSubscription;
	private bool _initialized;
	private bool _disposed;

	public RecipeMutationCoordinator(
		DomainFacade domainFacade,
		AppConfiguration appConfiguration,
		RecipeQueryService queryService,
		MessagePanelViewModel messagePanel)
	{
		_domainFacade = domainFacade;
		_appConfiguration = appConfiguration;
		_queryService = queryService;
		_messagePanel = messagePanel;
		_stepCoordinator = new RecipeStepCoordinator(
			domainFacade,
			() => _queryService.CurrentRecipe,
			result => _lastRecipeResult = result,
			index => SuggestedSelection = index,
			signal => _stateChanged.OnNext(signal),
			RebuildMessagePanel);
	}

	public IObservable<MutationSignal> StateChanged => _stateChanged;
	public IObservable<(Recipe Local, Recipe Plc)> PlcRecipeConflictDetected => _plcRecipeConflictDetected;
	public IObservable<Result<PlcSessionSnapshot>> PlcStateChanged => _plcStateChanged;

	public int? SuggestedSelection { get; private set; }

	public Recipe CurrentRecipe => _queryService.CurrentRecipe;

	public RecipeSnapshot Snapshot => _queryService.Snapshot;

	public bool IsDirty => _queryService.IsDirty;
	public bool CanUndo => _queryService.CanUndo;
	public bool CanRedo => _queryService.CanRedo;
	public bool IsConnected => _queryService.IsConnected;

	public IObservable<PlcExecutionInfo> ExecutionState => _queryService.ExecutionState;
	public bool IsRecipeActive => _queryService.IsRecipeActive;
	public bool IsSyncEnabled => _queryService.IsSyncEnabled;

	public RecipeQueryService QueryService => _queryService;

	public RecipeMutationCoordinator Initialize()
	{
		if (_initialized)
		{
			throw new InvalidOperationException("RecipeMutationCoordinator has already been initialized.");
		}

		_initialized = true;

		_domainFacade.PlcRecipeConflictDetected += OnPlcRecipeConflictDetected;

		_plcStateSubscription = _domainFacade.PlcState
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

		_domainFacade.PlcRecipeConflictDetected -= OnPlcRecipeConflictDetected;

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
		return _domainFacade.EnableSync(_appConfiguration.PlcConfiguration);
	}

	public Task DisableSync()
	{
		return _domainFacade.DisableSync();
	}

	public async Task<Result> LoadRecipeFromPlcAsync()
	{
		var result = await _domainFacade.LoadRecipeFromPlcAsync();

		_lastRecipeResult = result;

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
		_domainFacade.ResolveConflict(keepLocal);
	}

	public void AppendStep(int actionId)
	{
		_stepCoordinator.AppendStep(actionId);
	}

	public void InsertStep(int index, int actionId)
	{
		_stepCoordinator.InsertStep(index, actionId);
	}

	public void RemoveStep(int index)
	{
		_stepCoordinator.RemoveStep(index);
	}

	public void RemoveSteps(IReadOnlyList<int> indices)
	{
		_stepCoordinator.RemoveSteps(indices);
	}

	public void InsertSteps(int startIndex, IReadOnlyList<Step> steps)
	{
		_stepCoordinator.InsertSteps(startIndex, steps);
	}

	public void ChangeStepAction(int stepIndex, int newActionId)
	{
		_stepCoordinator.ChangeStepAction(stepIndex, newActionId);
	}

	public void UpdateStepProperty(int stepIndex, string columnKey, string value)
	{
		_stepCoordinator.UpdateStepProperty(stepIndex, columnKey, value);
	}

	public void Undo()
	{
		_stepCoordinator.Undo();
	}

	public void Redo()
	{
		_stepCoordinator.Redo();
	}

	public void NewRecipe()
	{
		_stepCoordinator.NewRecipe();
	}

	public async Task<Result> LoadRecipeAsync(string filePath)
	{
		var result = await _domainFacade.LoadRecipeAsync(filePath);

		_lastRecipeResult = result;

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
		await _domainFacade.SaveRecipeAsync(filePath);
		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.MetadataChanged());
	}

	private void OnPlcStateChanged(Result<PlcSessionSnapshot> result)
	{
		if (_disposed)
		{
			return;
		}

		_lastPlcState = result;
		_plcStateChanged.OnNext(result);
		RebuildMessagePanel();
	}

	private void OnPlcRecipeConflictDetected(Recipe local, Recipe plc)
	{
		Avalonia.Threading.Dispatcher.UIThread.Post(() =>
		{
			if (_disposed)
			{
				return;
			}
			_plcRecipeConflictDetected.OnNext((local, plc));
		});
	}

	private void RebuildMessagePanel()
	{
		var combinedReasons = _lastRecipeResult.Reasons.Concat(_lastPlcState.Reasons);
		_messagePanel.RefreshReasons(combinedReasons);
	}
}

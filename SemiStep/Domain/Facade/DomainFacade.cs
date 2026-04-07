using System.Collections.Immutable;

using Domain.Helpers;
using Domain.State;

using FluentResults;

using Serilog;

using TypesShared.Config;
using TypesShared.Core;
using TypesShared.Domain;
using TypesShared.Plc;

namespace Domain.Facade;

public sealed class DomainFacade : IDisposable
{
	private readonly AppConfiguration _appConfiguration;
	private readonly IClipboardService _clipboardService;
	private readonly ConfigRegistry _configRegistry;
	private readonly IS7Service _connectionService;
	private readonly ICoreService _coreService;
	private readonly ICsvService _csvService;
	private readonly RecipeHistoryManager _historyManager;
	private readonly ImportedRecipeValidator _importedRecipeValidator;
	private readonly IPropertyParser _propertyParser;
	private readonly RecipeStateManager _stateManager;
	private readonly IPlcSyncService _syncService;
	private Action<PlcConnectionState>? _connectionStateChangedRelay;

	private bool _disposed;
	private bool _isSyncEnabled;
	private Recipe? _pendingPlcRecipe;

	internal DomainFacade(
		AppConfiguration appConfiguration,
		ConfigRegistry configRegistry,
		ICoreService coreService,
		RecipeStateManager stateManager,
		RecipeHistoryManager historyManager,
		ICsvService csvService,
		IS7Service connectionService,
		IClipboardService clipboardService,
		ImportedRecipeValidator importedRecipeValidator,
		IPropertyParser propertyParser,
		IPlcSyncService syncService)
	{
		_appConfiguration = appConfiguration;
		_configRegistry = configRegistry;
		_coreService = coreService;
		_stateManager = stateManager;
		_historyManager = historyManager;
		_csvService = csvService;
		_connectionService = connectionService;
		_clipboardService = clipboardService;
		_importedRecipeValidator = importedRecipeValidator;
		_propertyParser = propertyParser;
		_syncService = syncService;
	}

	public Recipe CurrentRecipe => _stateManager.Current;
	public Recipe LastValidRecipe => _stateManager.LastValidRecipe;
	public bool IsDirty => _stateManager.IsDirty;
	public bool IsValid => _stateManager.IsValid;
	public Result<RecipeSnapshot> Snapshot => _stateManager.LatestSnapshot ?? RecipeSnapshot.Empty;

	public bool CanUndo => _historyManager.CanUndo;
	public bool CanRedo => _historyManager.CanRedo;

	public bool IsSyncEnabled => _isSyncEnabled;
	public bool IsConnected => _connectionService.IsConnected;
	public bool IsRecipeActive => _connectionService.IsRecipeActive;
	public IObservable<PlcExecutionInfo> ExecutionState => _connectionService.ExecutionState;

	public PlcSyncStatus SyncStatus => _syncService.Status;
	public string? SyncLastError => _syncService.LastError;
	public DateTimeOffset? LastSyncTime => _syncService.LastSyncTime;

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (_connectionStateChangedRelay is not null)
		{
			_connectionService.StateChanged -= _connectionStateChangedRelay;
		}
	}

	public static CellState GetCellState(GridColumnDefinition column, ActionDefinition action)
	{
		return CellStateResolver.GetCellState(column, action);
	}

	public void Initialize()
	{
		SetNewRecipe();

		_connectionStateChangedRelay = state =>
		{
			ConnectionStateChanged?.Invoke(state);

			if (state == PlcConnectionState.Disconnected && _isSyncEnabled)
			{
				_syncService.Reset();
			}
			else if (state == PlcConnectionState.Connected && _isSyncEnabled)
			{
				_ = PerformReconnectReconciliationAsync().ContinueWith(
					t => Log.Error(t.Exception, "Unhandled error in reconnect reconciliation"),
					TaskContinuationOptions.OnlyOnFaulted);
			}
		};
		_connectionService.StateChanged += _connectionStateChangedRelay;
	}

	public Result AppendStep(int actionId)
	{
		var snapshot = _coreService.AppendStep(CurrentRecipe, actionId);

		return ApplyIfSucceeded(snapshot);
	}

	public Result InsertStep(int index, int actionId)
	{
		var snapshot = _coreService.InsertStep(CurrentRecipe, index, actionId);

		return ApplyIfSucceeded(snapshot);
	}

	public Result RemoveStep(int index)
	{
		var snapshot = _coreService.RemoveStep(CurrentRecipe, index);

		return ApplyIfSucceeded(snapshot);
	}

	public Result InsertSteps(int startIndex, IReadOnlyList<Step> steps)
	{
		var snapshot = _coreService.InsertSteps(CurrentRecipe, startIndex, steps);

		return ApplyIfSucceeded(snapshot);
	}

	public Result RemoveSteps(IReadOnlyList<int> indices)
	{
		var snapshot = _coreService.RemoveSteps(CurrentRecipe, indices);

		return ApplyIfSucceeded(snapshot);
	}

	public Result ChangeStepAction(int stepIndex, int newActionId)
	{
		var snapshot = _coreService.ChangeStepAction(CurrentRecipe, stepIndex, newActionId);

		return ApplyIfSucceeded(snapshot);
	}

	public Result UpdateStepProperty(int stepIndex, string columnKey, string value)
	{
		var propertyResult = ResolvePropertyDefinition(stepIndex, columnKey);
		if (propertyResult.IsFailed)
		{
			return propertyResult.ToResult();
		}

		var parseResult = _propertyParser.Parse(value, propertyResult.Value);
		if (parseResult.IsFailed)
		{
			return parseResult.ToResult();
		}

		var snapshot = _coreService.UpdateStepProperty(
			CurrentRecipe, stepIndex, columnKey, parseResult.Value);

		return ApplyIfSucceeded(snapshot);
	}

	public Result Undo()
	{
		var previous = _historyManager.Undo(_stateManager.Current);
		if (previous is null)
		{
			return Result.Fail("No state to undo to");
		}

		var snapshot = _coreService.AnalyzeRecipe(previous);
		_stateManager.Update(snapshot);

		if (snapshot.IsFailed)
		{
			return snapshot.ToResult();
		}

		if (_isSyncEnabled)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
		}

		return Result.Ok().WithReasons(snapshot.Reasons);
	}

	public Result Redo()
	{
		var next = _historyManager.Redo(_stateManager.Current);
		if (next is null)
		{
			return Result.Fail("No state to redo to");
		}

		var snapshot = _coreService.AnalyzeRecipe(next);
		_stateManager.Update(snapshot);

		if (snapshot.IsFailed)
		{
			return snapshot.ToResult();
		}

		if (_isSyncEnabled)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
		}

		return Result.Ok().WithReasons(snapshot.Reasons);
	}

	public async Task<Result> LoadRecipeAsync(
		string filePath,
		CancellationToken ct = default)
	{
		var loadResult = await _csvService.LoadAsync(filePath, ct);
		if (loadResult.IsFailed)
		{
			return loadResult.ToResult();
		}

		var validationResult = _importedRecipeValidator.Validate(loadResult.Value);
		if (validationResult.IsFailed)
		{
			return validationResult;
		}

		_historyManager.Clear();
		var snapshot = _coreService.AnalyzeRecipe(loadResult.Value);
		_stateManager.Update(snapshot);
		_stateManager.MarkSaved();

		if (snapshot.IsFailed)
		{
			return snapshot.ToResult();
		}

		if (_isSyncEnabled)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
		}

		return Result.Ok().WithReasons(snapshot.Reasons);
	}

	public async Task SaveRecipeAsync(
		string filePath,
		CancellationToken ct = default)
	{
		await _csvService.SaveAsync(_stateManager.Current, filePath, ct);
		_stateManager.MarkSaved();
	}

	public void MarkSaved()
	{
		_stateManager.MarkSaved();
	}

	private Result ApplyIfSucceeded(Result<RecipeSnapshot> snapshot)
	{
		if (snapshot.IsFailed)
		{
			return snapshot.ToResult();
		}

		_historyManager.Push(_stateManager.Current);
		_stateManager.Update(snapshot);

		if (_isSyncEnabled)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
		}

		return Result.Ok().WithReasons(snapshot.Reasons);
	}

	private Result<PropertyTypeDefinition> ResolvePropertyDefinition(
		int stepIndex,
		string columnKey)
	{
		var recipe = _stateManager.Current;

		var validationResult = ValidateStepIndex(stepIndex);
		if (validationResult.IsFailed)
		{
			return validationResult.ToResult<PropertyTypeDefinition>();
		}

		return _configRegistry.ResolvePropertyType(recipe, stepIndex, columnKey);
	}

	public string SerializeStepsForClipboard(IReadOnlyList<Step> steps)
	{
		var recipe = new Recipe(steps.ToImmutableList());

		return _clipboardService.SerializeSteps(recipe);
	}

	public Result<Recipe> DeserializeStepsFromClipboard(string csvBody)
	{
		var result = _clipboardService.DeserializeSteps(csvBody);
		if (result.IsFailed)
		{
			return result;
		}

		var validationResult = _importedRecipeValidator.Validate(result.Value);
		if (validationResult.IsFailed)
		{
			return validationResult.ToResult<Recipe>();
		}

		return result;
	}

	public string? LastConnectionError { get; private set; }

	public event Action<PlcConnectionState>? ConnectionStateChanged;
	public event Action<Recipe, Recipe>? PlcRecipeConflictDetected;

	public async Task<Result> EnableSync(PlcConfiguration config)
	{
		if (_isSyncEnabled)
		{
			return Result.Ok();
		}

		try
		{
			LastConnectionError = null;
			_isSyncEnabled = true;
			await _connectionService.ConnectAsync(config.Connection);
		}
		catch (Exception ex)
		{
			_isSyncEnabled = false;
			LastConnectionError = ex.Message;
			Log.Warning("PLC connection failed: {Message}", ex.Message);
			return Result.Fail(ex.Message);
		}

		return Result.Ok();
	}

	public async Task DisableSync()
	{
		_isSyncEnabled = false;
		_syncService.Reset();

		try
		{
			await _connectionService.DisconnectAsync();
		}
		catch (Exception ex)
		{
			Log.Warning("Error while disconnecting from PLC: {Message}", ex.Message);
		}
	}

	public async Task<Result> LoadRecipeFromPlcAsync(CancellationToken ct = default)
	{
		var loadResult = await _connectionService.ReadRecipeFromPlcAsync(ct);
		if (loadResult.IsFailed)
		{
			return loadResult.ToResult();
		}

		var validationResult = _importedRecipeValidator.Validate(loadResult.Value);
		if (validationResult.IsFailed)
		{
			return validationResult;
		}

		_historyManager.Clear();
		var snapshot = _coreService.AnalyzeRecipe(loadResult.Value);
		_stateManager.Update(snapshot);

		if (snapshot.IsFailed)
		{
			return snapshot.ToResult();
		}

		if (_isSyncEnabled)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
		}

		return Result.Ok().WithReasons(snapshot.Reasons);
	}

	public void ResolveConflict(bool keepLocal)
	{
		if (keepLocal)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
		}
		else
		{
			if (_pendingPlcRecipe is not null)
			{
				LoadPlcRecipeIntoState(_pendingPlcRecipe);
				_pendingPlcRecipe = null;
			}
		}
	}

	public void SetNewRecipe()
	{
		_historyManager.Clear();
		_stateManager.Reset();

		var snapshot = _coreService.AnalyzeRecipe(Recipe.Empty);
		_stateManager.Update(snapshot);

		if (snapshot.IsFailed)
		{
			Log.Warning("Empty recipe analysis unexpectedly failed: {Errors}",
				string.Join("; ", snapshot.Errors.Select(e => e.Message)));
		}
	}

	private Result ValidateStepIndex(int stepIndex)
	{
		var recipe = _stateManager.Current;
		if (stepIndex < 0 || stepIndex >= recipe.Steps.Count)
		{
			return Result.Fail($"Step index {stepIndex} is out of range for recipe with {recipe.Steps.Count} steps");
		}
		return Result.Ok();
	}

	private async Task PerformReconnectReconciliationAsync(CancellationToken ct = default)
	{
		var managingAreaResult = await _connectionService.ReadManagingAreaAsync(ct);
		if (managingAreaResult.IsFailed)
		{
			Log.Warning(
				"Could not read managing area during reconnect reconciliation: {Errors}",
				string.Join("; ", managingAreaResult.Errors.Select(e => e.Message)));
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
			return;
		}

		if (!managingAreaResult.Value.Committed)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
			return;
		}

		var plcRecipeResult = await _connectionService.ReadRecipeFromPlcAsync(ct);
		if (plcRecipeResult.IsFailed)
		{
			Log.Warning(
				"Could not read PLC recipe during reconciliation: {Errors}",
				string.Join("; ", plcRecipeResult.Errors.Select(e => e.Message)));
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
			return;
		}

		var plcRecipe = plcRecipeResult.Value;
		var localRecipe = _stateManager.Current;

		if (localRecipe.Steps.Count == 0 && plcRecipe.Steps.Count > 0)
		{
			LoadPlcRecipeIntoState(plcRecipe);
			return;
		}

		if (plcRecipe.Steps.Count > 0 && !localRecipe.Equals(plcRecipe))
		{
			_pendingPlcRecipe = plcRecipe;
			PlcRecipeConflictDetected?.Invoke(localRecipe, plcRecipe);
			return;
		}

		_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
	}

	private void LoadPlcRecipeIntoState(Recipe recipe)
	{
		_historyManager.Clear();
		var snapshot = _coreService.AnalyzeRecipe(recipe);
		_stateManager.Update(snapshot);

		if (snapshot.IsFailed)
		{
			Log.Warning(
				"PLC recipe analysis produced errors: {Errors}",
				string.Join("; ", snapshot.Errors.Select(e => e.Message)));
		}

		if (_isSyncEnabled)
		{
			_syncService.NotifyRecipeChanged(_stateManager.Current, _stateManager.IsValid);
		}
	}
}

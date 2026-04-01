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
	private Action<PlcConnectionState>? _connectionStateChangedRelay;

	private bool _disposed;

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
		IPropertyParser propertyParser)
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
	}

	public Recipe CurrentRecipe => _stateManager.Current;
	public Recipe LastValidRecipe => _stateManager.LastValidRecipe;
	public bool IsDirty => _stateManager.IsDirty;
	public bool IsValid => _stateManager.IsValid;
	public Result<RecipeSnapshot> Snapshot => _stateManager.LatestSnapshot ?? RecipeSnapshot.Empty;

	public bool CanUndo => _historyManager.CanUndo;
	public bool CanRedo => _historyManager.CanRedo;

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

		_connectionStateChangedRelay = _ => ConnectionStateChanged?.Invoke();
		_connectionService.StateChanged += _connectionStateChangedRelay;
		StartPlcConnection(_appConfiguration.PlcConfiguration);
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

	public bool IsConnected => _connectionService.IsConnected;
	public string? LastConnectionError { get; private set; }

	public event Action? ConnectionStateChanged;

	public void StartPlcConnection(PlcConfiguration plcConfiguration)
	{
		if (IsConnected)
		{
			Log.Warning("PLC connection is already established");
			return;
		}

		Task.Run(async () =>
		{
			try
			{
				LastConnectionError = null;
				await _connectionService.ConnectAsync(plcConfiguration.Connection);
			}
			catch (Exception ex)
			{
				LastConnectionError = ex.Message;
				Log.Warning(ex, "Initial PLC connection failed, auto-reconnect will retry");
			}
		});
	}

	public void StopPlcConnection()
	{
		if (!IsConnected)
		{
			Log.Warning("PLC connection is not established");
			return;
		}

		Task.Run(async () =>
		{
			try
			{
				await _connectionService.DisconnectAsync();
				await _connectionService.DisposeAsync();
			}
			catch (Exception ex)
			{
				Log.Warning(ex, "Error while disconnecting PLC");
			}
		});
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
}

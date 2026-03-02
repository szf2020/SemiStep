using Core.Entities;

using Domain.Services;

using ReactiveUI;

using Shared.Entities;
using Shared.Registries;

namespace UI.ViewModels;

public class RecipeRowViewModel : ReactiveObject, IDisposable
{
	private readonly List<Action> _cleanupActions = [];
	private readonly IGroupRegistry _groupRegistry;
	private readonly IColumnRegistry _columnRegistry;
	private readonly Action<RecipeRowViewModel, string, object?> _onPropertyChanged;
	private readonly Action<RecipeRowViewModel, int> _onActionChanged;

	private ActionDefinition _action;
	private IReadOnlyDictionary<string, CellState>? _cellStatesCache;
	private bool _disposed;
	private bool _isExecuting;
	private int _stepNumber;
	private Step _step;

	public RecipeRowViewModel(
		int stepNumber,
		Step step,
		ActionDefinition action,
		IGroupRegistry groupRegistry,
		IColumnRegistry columnRegistry,
		Action<RecipeRowViewModel, string, object?> onPropertyChanged,
		Action<RecipeRowViewModel, int> onActionChanged)
	{
		_stepNumber = stepNumber;
		_step = step;
		_action = action;
		_groupRegistry = groupRegistry;
		_columnRegistry = columnRegistry;
		_onPropertyChanged = onPropertyChanged;
		_onActionChanged = onActionChanged;
	}

	public int StepNumber
	{
		get => _stepNumber;
		private set => this.RaiseAndSetIfChanged(ref _stepNumber, value);
	}

	public int ActionId => _step.ActionKey;

	public string ActionName => _action.UiName;

	public bool IsExecuting
	{
		get => _isExecuting;
		set => this.RaiseAndSetIfChanged(ref _isExecuting, value);
	}

	public IReadOnlyDictionary<string, CellState> CellStates
	{
		get
		{
			if (_cellStatesCache is not null)
			{
				return _cellStatesCache;
			}

			var states = new Dictionary<string, CellState>();
			foreach (var columnDef in _columnRegistry.GetAll())
			{
				states[columnDef.Key] = CellStateResolver.GetCellState(columnDef, _action);
			}

			_cellStatesCache = states;

			return _cellStatesCache;
		}
	}

	public object? this[string columnKey]
	{
		get => GetPropertyValue(columnKey);
		set => SetPropertyValue(columnKey, value);
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		foreach (var cleanup in _cleanupActions)
		{
			try
			{
				cleanup();
			}
			catch
			{
				// Ignore cleanup errors
			}
		}

		_cleanupActions.Clear();
	}

	public void UpdateStep(Step newStep)
	{
		_step = newStep;
	}

	public void UpdateStep(Step newStep, ActionDefinition newAction)
	{
		_step = newStep;
		_action = newAction;
		_cellStatesCache = null;
		this.RaisePropertyChanged(nameof(ActionId));
		this.RaisePropertyChanged(nameof(ActionName));
		this.RaisePropertyChanged(nameof(CellStates));
	}

	public void UpdateStepNumber(int newNumber)
	{
		StepNumber = newNumber;
	}

	public object? GetPropertyValue(string columnKey)
	{
		if (columnKey is "action")
		{
			return ActionId;
		}

		var columnId = new ColumnId(columnKey);
		if (_step.Properties.TryGetValue(columnId, out var propertyValue))
		{
			return propertyValue.Value;
		}

		return null;
	}

	public void SetPropertyValue(string columnKey, object? value)
	{
		if (columnKey == "action")
		{
			if (value is int actionId)
			{
				_onActionChanged(this, actionId);
			}

			return;
		}

		_onPropertyChanged(this, columnKey, value);
		this.RaisePropertyChanged(columnKey);
		this.RaisePropertyChanged("Item[]");
	}

	public IReadOnlyDictionary<int, string>? GetGroupItemsForColumn(string columnKey)
	{
		var actionColumn = _action.Columns.FirstOrDefault(c => c.Key == columnKey);
		if (actionColumn?.GroupName is null)
		{
			return null;
		}

		if (!_groupRegistry.GroupExists(actionColumn.GroupName))
		{
			return null;
		}

		return _groupRegistry.GetGroup(actionColumn.GroupName).Items;
	}

	public void InvalidateCellStates()
	{
		_cellStatesCache = null;
		this.RaisePropertyChanged(nameof(CellStates));
	}

	/// <summary>
	/// Registers a cleanup action to be called when this row is disposed.
	/// Used to unsubscribe event handlers.
	/// </summary>
	public void RegisterCleanup(Action cleanupAction)
	{
		if (_disposed)
		{
			// Already disposed, execute immediately
			cleanupAction();

			return;
		}

		_cleanupActions.Add(cleanupAction);
	}
}

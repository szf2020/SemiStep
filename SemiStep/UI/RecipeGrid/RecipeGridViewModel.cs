using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using ReactiveUI;

using TypesShared.Config;
using TypesShared.Core;
using TypesShared.Plc;

using UI.Coordinator;
using UI.MessageService;

namespace UI.RecipeGrid;

public class RecipeGridViewModel : ReactiveObject, IDisposable
{
	private readonly ObservableAsPropertyHelper<bool> _canDeleteStep;
	private readonly ObservableAsPropertyHelper<bool> _isReadOnly;
	private readonly RecipeMutationCoordinator _coordinator;
	private readonly CompositeDisposable _disposables = new();
	private readonly MessagePanelViewModel _messagePanel;

	private int _selectedRowIndex = -1;
	private IReadOnlyList<int> _selectedRowIndices = [];

	public RecipeGridViewModel(
		RecipeMutationCoordinator coordinator,
		ConfigRegistry configRegistry,
		MessagePanelViewModel messagePanel)
	{
		_coordinator = coordinator;
		ConfigRegistry = configRegistry;
		_messagePanel = messagePanel;

		RecipeRows = new ObservableCollection<RecipeRowViewModel>();

		_canDeleteStep = this
			.WhenAnyValue(x => x.SelectedRowIndices)
			.Select(indices => indices.Count > 0)
			.ToProperty(this, x => x.CanDeleteStep)
			.DisposeWith(_disposables);

		_isReadOnly = coordinator.ExecutionState
			.ObserveOn(RxApp.MainThreadScheduler)
			.Select(info => info.RecipeActive)
			.ToProperty(this, x => x.IsReadOnly)
			.DisposeWith(_disposables);

		coordinator.ExecutionState
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(OnExecutionStateChanged)
			.DisposeWith(_disposables);

		coordinator.StateChanged.Subscribe(OnStateChange).DisposeWith(_disposables);
	}

	internal ConfigRegistry ConfigRegistry { get; }

	public ObservableCollection<RecipeRowViewModel> RecipeRows { get; }

	public bool CanDeleteStep => _canDeleteStep.Value;

	public bool IsReadOnly => _isReadOnly.Value;

	public int SelectedRowIndex
	{
		get => _selectedRowIndex;
		set => this.RaiseAndSetIfChanged(ref _selectedRowIndex, value);
	}

	public IReadOnlyList<int> SelectedRowIndices
	{
		get => _selectedRowIndices;
		set => this.RaiseAndSetIfChanged(ref _selectedRowIndices, value);
	}

	public void Dispose()
	{
		DisposeAllRows();
		_disposables.Dispose();
		GC.SuppressFinalize(this);
	}

	public void Initialize()
	{
		FullRebuild(_coordinator.CurrentRecipe);
	}

	private void OnExecutionStateChanged(PlcExecutionInfo info)
	{
		for (var i = 0; i < RecipeRows.Count; i++)
		{
			if (info.RecipeActive)
			{
				RecipeRows[i].IsCurrentStep = i == info.ActualLine;
				RecipeRows[i].IsPastStep = i < info.ActualLine;
			}
			else
			{
				RecipeRows[i].IsCurrentStep = false;
				RecipeRows[i].IsPastStep = false;
			}
		}
	}

	private void OnStateChange(MutationSignal signal)
	{
		var recipe = _coordinator.CurrentRecipe;

		switch (signal)
		{
			case MutationSignal.PropertyUpdated(var stepIndex):
				UpdateSingleRowInPlace(recipe, stepIndex);

				break;

			case MutationSignal.StepAppended(var index):
				AppendRow(recipe, index);

				break;

			case MutationSignal.StepsInserted(var startIndex, var count):
				InsertRows(recipe, startIndex, count);

				break;

			case MutationSignal.StepRemoved(var removedIndex):
				RemoveRow(removedIndex);

				break;

			case MutationSignal.StepsRemoved(var removedIndices):
				RemoveRows(removedIndices);

				break;

			case MutationSignal.StepActionChanged(var stepIndex):
				RebuildRow(recipe, stepIndex);

				break;

			case MutationSignal.RecipeReplaced:
				FullRebuild(recipe);

				break;

			case MutationSignal.MetadataChanged:
				break;

			default:
				FullRebuild(recipe);

				break;
		}

		ApplyPostMutationUpdates(signal);
	}

	private void ApplyPostMutationUpdates(MutationSignal signal)
	{
		if (signal is not MutationSignal.MetadataChanged)
		{
			RefreshStepStartTimes();
		}

		var suggested = _coordinator.ConsumeSuggestedSelection();
		if (suggested.HasValue)
		{
			SelectedRowIndex = suggested.Value;
		}
	}

	private void OnCellValueChanged(RecipeRowViewModel row, string columnKey, string? value)
	{
		if (value is null)
		{
			return;
		}

		var stepIndex = RecipeRows.IndexOf(row);
		if (stepIndex < 0)
		{
			return;
		}

		try
		{
			_coordinator.UpdateStepProperty(stepIndex, columnKey, value);
		}
		catch (Exception ex)
		{
			_messagePanel.AddError($"Step {stepIndex + 1}: {ex.Message}", "RecipeGrid");
		}
	}

	private void OnActionChanged(RecipeRowViewModel row, int newActionId)
	{
		var stepIndex = RecipeRows.IndexOf(row);
		if (stepIndex < 0)
		{
			return;
		}

		try
		{
			_coordinator.ChangeStepAction(stepIndex, newActionId);
		}
		catch (Exception ex)
		{
			_messagePanel.AddError(
				$"Step {stepIndex + 1}: Failed to change action - {ex.Message}", "RecipeGrid");
		}
	}

	private void UpdateAllRowsInPlace(Recipe recipe)
	{
		Debug.Assert(RecipeRows.Count == recipe.StepCount,
			"Row count mismatch: grid rows and recipe steps are out of sync.");

		for (var i = 0; i < recipe.StepCount; i++)
		{
			RecipeRows[i].UpdateStep(recipe.Steps[i]);
		}
	}

	private void UpdateSingleRowInPlace(Recipe recipe, int stepIndex)
	{
		if (stepIndex < 0 || stepIndex >= RecipeRows.Count)
		{
			UpdateAllRowsInPlace(recipe);

			return;
		}

		RecipeRows[stepIndex].UpdateStep(recipe.Steps[stepIndex]);
	}

	private void AppendRow(Recipe recipe, int index)
	{
		var step = recipe.Steps[index];
		var action = ConfigRegistry.GetAction(step.ActionKey).Value;
		RecipeRows.Add(CreateRowViewModel(step, action, index + 1));
	}

	private void InsertRows(Recipe recipe, int startIndex, int count)
	{
		for (var i = 0; i < count; i++)
		{
			var index = startIndex + i;
			var step = recipe.Steps[index];
			var action = ConfigRegistry.GetAction(step.ActionKey).Value;
			RecipeRows.Insert(index, CreateRowViewModel(step, action, index + 1));
		}

		RenumberRows(startIndex + count);
	}

	private void RemoveRow(int removedIndex)
	{
		RecipeRows[removedIndex].Dispose();
		RecipeRows.RemoveAt(removedIndex);
		RenumberRows(removedIndex);
	}

	private void RemoveRows(IReadOnlyList<int> removedIndices)
	{
		if (removedIndices.Count == 0)
		{
			return;
		}

		foreach (var index in removedIndices.OrderByDescending(i => i))
		{
			RecipeRows[index].Dispose();
			RecipeRows.RemoveAt(index);
		}

		var minIndex = removedIndices.Min();
		RenumberRows(minIndex);
	}

	private void RebuildRow(Recipe recipe, int stepIndex)
	{
		RecipeRows[stepIndex].Dispose();
		var step = recipe.Steps[stepIndex];
		var action = ConfigRegistry.GetAction(step.ActionKey).Value;
		RecipeRows[stepIndex] = CreateRowViewModel(step, action, stepIndex + 1);
	}

	private void RenumberRows(int fromIndex)
	{
		for (var i = fromIndex; i < RecipeRows.Count; i++)
		{
			RecipeRows[i].UpdateStepNumber(i + 1);
		}
	}

	private void FullRebuild(Recipe recipe)
	{
		DisposeAllRows();
		RecipeRows.Clear();

		for (var i = 0; i < recipe.StepCount; i++)
		{
			var step = recipe.Steps[i];
			var action = ConfigRegistry.GetAction(step.ActionKey).Value;
			RecipeRows.Add(CreateRowViewModel(step, action, i + 1));
		}
	}

	private void RefreshStepStartTimes()
	{
		var stepStartTimes = _coordinator.Snapshot.StepStartTimes;
		for (var i = 0; i < RecipeRows.Count; i++)
		{
			var rawSeconds = stepStartTimes.TryGetValue(i, out var time)
				? time.TotalSeconds.ToString(CultureInfo.InvariantCulture)
				: string.Empty;
			RecipeRows[i].UpdateStepStartTime(rawSeconds);
		}
	}

	public List<Step> CollectSelectedSteps()
	{
		var recipe = _coordinator.CurrentRecipe;

		return _selectedRowIndices
			.OrderBy(i => i)
			.Select(i => recipe.Steps[i])
			.ToList();
	}

	private RecipeRowViewModel CreateRowViewModel(
		Step step,
		ActionDefinition action,
		int stepNumber)
	{
		var cellStates = new Dictionary<string, CellState>();
		foreach (var col in ConfigRegistry.GetAllColumns())
		{
			cellStates[col.Key] = _coordinator.QueryService.GetCellState(col, action);
		}

		var row = new RecipeRowViewModel(
			stepNumber,
			step,
			action,
			ConfigRegistry,
			cellStates);

		row.PropertyValueChanged += (columnKey, value) => OnCellValueChanged(row, columnKey, value);
		row.ActionChanged += actionId => OnActionChanged(row, actionId);

		return row;
	}

	private void DisposeAllRows()
	{
		foreach (var row in RecipeRows)
		{
			row.Dispose();
		}
	}
}

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using Domain.Facade;

using ReactiveUI;

using Shared;
using Shared.Registries;

namespace UI.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
	private readonly DomainFacade _domainFacade;
	private readonly IActionRegistry _actionRegistry;
	private readonly IGroupRegistry _groupRegistry;
	private readonly IColumnRegistry _columnRegistry;
	private int _selectedRowIndex = -1;
	private string? _currentFileName;

	public MainWindowViewModel(
		DomainFacade domainFacade,
		IActionRegistry actionRegistry,
		IGroupRegistry groupRegistry,
		IColumnRegistry columnRegistry)
	{
		_domainFacade = domainFacade;
		_actionRegistry = actionRegistry;
		_groupRegistry = groupRegistry;
		_columnRegistry = columnRegistry;

		RecipeRows = new ObservableCollection<RecipeRowViewModel>();
		ValidationErrors = new ObservableCollection<string>();
		ValidationErrors.CollectionChanged += OnValidationErrorsChanged;

		// File dialog interactions
		OpenFileInteraction = new Interaction<Unit, string?>();
		SaveFileInteraction = new Interaction<string?, string?>();
		ShowMessageInteraction = new Interaction<(string Title, string Message), Unit>();

		AddStepCommand = ReactiveCommand.Create(AddStep);
		DeleteStepCommand = ReactiveCommand.Create(DeleteStep);
		SaveRecipeCommand = ReactiveCommand.CreateFromTask(SaveRecipeAsync);
		LoadRecipeCommand = ReactiveCommand.CreateFromTask(LoadRecipeAsync);
		NewRecipeCommand = ReactiveCommand.Create(NewRecipe);
		UndoCommand = ReactiveCommand.Create(Undo);
		RedoCommand = ReactiveCommand.Create(Redo);
		ExitCommand = ReactiveCommand.Create(Exit);
	}

	public IActionRegistry ActionRegistry => _actionRegistry;

	public IGroupRegistry GroupRegistry => _groupRegistry;

	public ObservableCollection<RecipeRowViewModel> RecipeRows { get; }

	public ObservableCollection<string> ValidationErrors { get; }

	public AppConfiguration? Configuration { get; private set; }

	// File dialog interactions - handled by the View
	public Interaction<Unit, string?> OpenFileInteraction { get; }

	public Interaction<string?, string?> SaveFileInteraction { get; }

	public Interaction<(string Title, string Message), Unit> ShowMessageInteraction { get; }

	public ReactiveCommand<Unit, Unit> AddStepCommand { get; }

	public ReactiveCommand<Unit, Unit> DeleteStepCommand { get; }

	public ReactiveCommand<Unit, Unit> SaveRecipeCommand { get; }

	public ReactiveCommand<Unit, Unit> LoadRecipeCommand { get; }

	public ReactiveCommand<Unit, Unit> NewRecipeCommand { get; }

	public ReactiveCommand<Unit, Unit> UndoCommand { get; }

	public ReactiveCommand<Unit, Unit> RedoCommand { get; }

	public ReactiveCommand<Unit, Unit> ExitCommand { get; }

	public string WindowTitle
	{
		get
		{
			var fileName = _currentFileName ?? "Untitled";
			var dirtyIndicator = IsDirty ? " *" : "";
			return $"SemiStep - {fileName}{dirtyIndicator}";
		}
	}

	public bool IsDirty => _domainFacade.IsDirty;

	public bool CanUndo => _domainFacade.CanUndo;

	public bool CanRedo => _domainFacade.CanRedo;

	public int SelectedRowIndex
	{
		get => _selectedRowIndex;
		set => this.RaiseAndSetIfChanged(ref _selectedRowIndex, value);
	}

	public bool CanDeleteStep => SelectedRowIndex >= 0;

	public bool IsConnectedToPlc => false;

	public string StatusText => IsDirty ? "Modified" : "Saved";

	public string ConnectionStatus => IsConnectedToPlc ? "Connected" : "Disconnected";

	public bool HasValidationErrors => ValidationErrors.Count > 0;

	public void Initialize(AppConfiguration configuration)
	{
		Configuration = configuration;
		RefreshRecipeRows();
	}

	private void AddStep()
	{
		var firstAction = _actionRegistry.GetAll().First();
		int newRowIndex;

		if (SelectedRowIndex >= 0)
		{
			// Insert after selected row
			newRowIndex = SelectedRowIndex + 1;
			_domainFacade.InsertStep(newRowIndex, firstAction.Id);
		}
		else
		{
			// Append to end
			newRowIndex = RecipeRows.Count; // Current count = index of new row after refresh
			_domainFacade.AppendStep(firstAction.Id);
		}

		RefreshRecipeRows();
		SelectedRowIndex = newRowIndex;
		RaiseStateChanged();
	}

	private void DeleteStep()
	{
		if (SelectedRowIndex < 0)
		{
			return;
		}

		var indexToDelete = SelectedRowIndex;
		_domainFacade.RemoveStep(indexToDelete);

		RefreshRecipeRows();

		// Keep selection at same position if possible, otherwise select the new last row
		if (RecipeRows.Count > 0)
		{
			SelectedRowIndex = Math.Min(indexToDelete, RecipeRows.Count - 1);
		}
		else
		{
			SelectedRowIndex = -1;
		}

		RaiseStateChanged();
	}

	private async Task SaveRecipeAsync()
	{
		// Show save file dialog
		var filePath = await SaveFileInteraction.Handle(null);
		if (filePath is null)
		{
			return; // User cancelled
		}

		// TODO: Implement file saving when backend is ready
		// For now, show "Not implemented" message
		await ShowMessageInteraction.Handle(("Save Recipe", "File saving is not yet implemented.\n\nSelected path: " + filePath));
	}

	private async Task LoadRecipeAsync()
	{
		// Show open file dialog
		var filePath = await OpenFileInteraction.Handle(Unit.Default);
		if (filePath is null)
		{
			return; // User cancelled
		}

		// TODO: Implement file loading when backend is ready
		// For now, show "Not implemented" message
		await ShowMessageInteraction.Handle(("Open Recipe", "File loading is not yet implemented.\n\nSelected path: " + filePath));
	}

	private void NewRecipe()
	{
		_domainFacade.NewRecipe();
		ValidationErrors.Clear();
		RefreshRecipeRows();
		RaiseStateChanged();
	}

	private void Undo()
	{
		var snapshot = _domainFacade.Undo();
		if (snapshot is not null)
		{
			RefreshRecipeRows();
			RaiseStateChanged();
		}
	}

	private void Redo()
	{
		var snapshot = _domainFacade.Redo();
		if (snapshot is not null)
		{
			RefreshRecipeRows();
			RaiseStateChanged();
		}
	}

	private void RefreshRecipeRows()
	{
		var recipe = _domainFacade.CurrentRecipe;

		// Dispose old rows to unsubscribe event handlers
		foreach (var row in RecipeRows)
		{
			row.Dispose();
		}

		RecipeRows.Clear();

		for (var i = 0; i < recipe.StepCount; i++)
		{
			var step = recipe.Steps[i];
			var action = _actionRegistry.GetAction(step.ActionKey);
			var rowVm = new RecipeRowViewModel(
				i + 1,
				step,
				action,
				_groupRegistry,
				_columnRegistry,
				OnCellValueChanged,
				OnActionChanged);
			RecipeRows.Add(rowVm);
		}
	}

	private static void Exit()
	{
		if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
		{
			lifetime.Shutdown();
		}
	}

	private void OnCellValueChanged(int stepIndex, string columnKey, object? value)
	{
		if (value is null)
		{
			return;
		}

		try
		{
			_domainFacade.UpdateStepProperty(stepIndex, columnKey, value);
		}
		catch (Exception ex)
		{
			ValidationErrors.Add($"Step {stepIndex + 1}: {ex.Message}");
		}

		RefreshRecipeRows();
		RaiseStateChanged();
	}

	private void OnActionChanged(int stepIndex, int newActionId)
	{
		try
		{
			_domainFacade.ChangeStepAction(stepIndex, newActionId);
			RefreshRecipeRows();
			RaiseStateChanged();
		}
		catch (Exception ex)
		{
			ValidationErrors.Add($"Step {stepIndex + 1}: Failed to change action - {ex.Message}");
		}
	}

	private void OnValidationErrorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		this.RaisePropertyChanged(nameof(HasValidationErrors));
	}

	private void RaiseStateChanged()
	{
		this.RaisePropertyChanged(nameof(IsDirty));
		this.RaisePropertyChanged(nameof(StatusText));
		this.RaisePropertyChanged(nameof(CanUndo));
		this.RaisePropertyChanged(nameof(CanRedo));
		this.RaisePropertyChanged(nameof(CanDeleteStep));
		this.RaisePropertyChanged(nameof(WindowTitle));
	}
}

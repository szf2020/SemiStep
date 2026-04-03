using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using ReactiveUI;

using UI.Clipboard;
using UI.Coordinator;
using UI.MessageService;
using UI.RecipeFile;
using UI.RecipeGrid;
using UI.ShutdownService;

namespace UI.MainWindow;

public class MainWindowViewModel : ReactiveObject, IDisposable
{
	private readonly RecipeMutationCoordinator _coordinator;
	private readonly CompositeDisposable _disposables = new();

	public MainWindowViewModel(
		RecipeMutationCoordinator coordinator,
		RecipeGridViewModel recipeGrid,
		RecipeCommandsViewModel recipeCommands,
		ClipboardViewModel clipboard,
		RecipeFileViewModel recipeFile,
		MessagePanelViewModel messagePanel,
		ColumnBuilder columnBuilder)
	{
		_coordinator = coordinator;
		RecipeGrid = recipeGrid;
		RecipeCommands = recipeCommands;
		Clipboard = clipboard;
		RecipeFile = recipeFile;
		MessagePanel = messagePanel;
		ColumnBuilder = columnBuilder;

		ExitCommand = ReactiveCommand.Create(ExecuteExit);

		_coordinator.StateChanged
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(_ => RaiseAllStateProperties())
			.DisposeWith(_disposables);
	}

	public RecipeGridViewModel RecipeGrid { get; }

	public RecipeCommandsViewModel RecipeCommands { get; }

	public ClipboardViewModel Clipboard { get; }

	public RecipeFileViewModel RecipeFile { get; }

	public MessagePanelViewModel MessagePanel { get; }

	public ColumnBuilder ColumnBuilder { get; }

	public ReactiveCommand<Unit, Unit> ExitCommand { get; }

	public bool IsConnectedToPlc => _coordinator.IsConnected;

	public string ConnectionStatus => IsConnectedToPlc ? "Connected" : "Disconnected";

	public string WindowTitle => BuildWindowTitle();

	public bool IsDirty => _coordinator.IsDirty;
	public bool CanUndo => _coordinator.CanUndo;
	public bool CanRedo => _coordinator.CanRedo;

	public string StatusText => IsDirty ? "Modified" : "Saved";

	public void Dispose()
	{
		_disposables.Dispose();
		GC.SuppressFinalize(this);
	}

	public void Initialize()
	{
		RecipeGrid.Initialize();
		RaiseAllStateProperties();
	}

	private static void ExecuteExit()
	{
		DesktopShutdownService.Shutdown();
	}

	private void RaiseAllStateProperties()
	{
		this.RaisePropertyChanged(nameof(WindowTitle));
		this.RaisePropertyChanged(nameof(IsDirty));
		this.RaisePropertyChanged(nameof(CanUndo));
		this.RaisePropertyChanged(nameof(CanRedo));
		this.RaisePropertyChanged(nameof(StatusText));
	}

	private string BuildWindowTitle()
	{
		var fileName = RecipeFile.CurrentFilePath is not null
			? Path.GetFileNameWithoutExtension(RecipeFile.CurrentFilePath)
			: "New Recipe";
		var dirtyIndicator = IsDirty ? " *" : "";

		return $"SemiStep - {fileName}{dirtyIndicator}";
	}
}

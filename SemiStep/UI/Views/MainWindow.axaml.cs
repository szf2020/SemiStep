using Avalonia.Controls;

using UI.Helpers;
using UI.ViewModels;

namespace UI.Views;

public partial class MainWindow : Window
{
	private ColumnBuilder? _columnBuilder;

	public MainWindow()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
	}

	private void OnDataContextChanged(object? sender, EventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel || viewModel.Configuration is null)
		{
			return;
		}


		_columnBuilder = new ColumnBuilder(
			viewModel.ActionRegistry);

		BuildGrid();
	}

	private void OnStylesChanged(object? sender, Shared.Entities.GridStyleOptions styles)
	{
		if (_columnBuilder is null)
		{
			return;
		}

		BuildGrid();
	}

	private void BuildGrid()
	{
		if (DataContext is not MainWindowViewModel viewModel || viewModel.Configuration is null)
		{
			return;
		}

		if (_columnBuilder is null)
		{
			return;
		}


		_columnBuilder.BuildColumnsFromConfiguration(RecipeGrid, viewModel.Configuration);
	}

	private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
	{
		if (DataContext is not MainWindowViewModel viewModel)
		{
			return;
		}

		foreach (var row in viewModel.RecipeRows)
		{
			row.IsSelected = RecipeGrid.SelectedItems?.Contains(row) ?? false;
		}
	}
}

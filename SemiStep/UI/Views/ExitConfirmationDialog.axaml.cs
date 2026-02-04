using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UI.Views;

public partial class ExitConfirmationDialog : Window
{
	public ExitConfirmationResult Result { get; private set; } = ExitConfirmationResult.Cancel;

	public ExitConfirmationDialog()
	{
		InitializeComponent();
	}

	private void OnSaveClick(object? sender, RoutedEventArgs e)
	{
		Result = ExitConfirmationResult.Save;
		Close();
	}

	private void OnDontSaveClick(object? sender, RoutedEventArgs e)
	{
		Result = ExitConfirmationResult.DontSave;
		Close();
	}

	private void OnCancelClick(object? sender, RoutedEventArgs e)
	{
		Result = ExitConfirmationResult.Cancel;
		Close();
	}
}

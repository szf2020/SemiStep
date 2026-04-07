using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UI.Plc;

internal partial class PlcConflictDialog : Window
{
	public bool KeepLocal { get; private set; }

	internal PlcConflictDialog()
	{
		InitializeComponent();
	}

	private void OnKeepLocalClick(object? sender, RoutedEventArgs e)
	{
		KeepLocal = true;
		Close();
	}

	private void OnLoadFromPlcClick(object? sender, RoutedEventArgs e)
	{
		KeepLocal = false;
		Close();
	}
}

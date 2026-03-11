using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace UI.Services;

internal class DesktopShutdownService : IShutdownService
{
	public void Shutdown()
	{
		if (Application.Current?.ApplicationLifetime
			is IClassicDesktopStyleApplicationLifetime lifetime)
		{
			lifetime.Shutdown();
		}
	}
}

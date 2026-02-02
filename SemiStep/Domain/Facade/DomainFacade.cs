using Domain.Services;

using Shared;
using Shared.Registries;

namespace Domain.Facade;

public sealed class DomainFacade(
	IActionRegistry actionRegistry,
	IPropertyRegistry propertyRegistry,
	IColumnRegistry columnRegistry,
	IGroupRegistry groupRegistry,
	CoreService coreService)
	: IDisposable
{
	private bool _disposed;

	public CoreService Core => coreService;


	public void Initialize(AppConfiguration appConfig)
	{
		actionRegistry.Initialize(appConfig.Actions);
		propertyRegistry.Initialize(appConfig.Properties);
		columnRegistry.Initialize(appConfig.Columns);
		groupRegistry.Initialize(appConfig.Groups);

		coreService.NewRecipe();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
	}
}

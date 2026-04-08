using Domain.Facade;

using Microsoft.Extensions.DependencyInjection;

using Tests.Core.Helpers;
using Tests.Helpers;

using TypesShared.Config;

using UI.Coordinator;
using UI.MessageService;
using UI.RecipeGrid;

using Xunit;

namespace Tests.UI.Helpers;

public sealed class UIFixture : IAsyncLifetime
{
	public DomainFacade Facade { get; private set; } = null!;
	public ConfigRegistry ConfigRegistry { get; private set; } = null!;
	public MessagePanelViewModel MessagePanel { get; private set; } = null!;
	public RecipeQueryService QueryService { get; private set; } = null!;
	public RecipeMutationCoordinator Coordinator { get; private set; } = null!;
	public RecipeGridViewModel Grid { get; private set; } = null!;

	public async Task InitializeAsync()
	{
		var (services, facade) = await CoreTestHelper.BuildAsync("WithGroups");
		Facade = facade;
		ConfigRegistry = services.GetRequiredService<ConfigRegistry>();
		MessagePanel = new MessagePanelViewModel();
		QueryService = new RecipeQueryService(Facade, ConfigRegistry);
		var appConfiguration = services.GetRequiredService<AppConfiguration>();
		Coordinator = new RecipeMutationCoordinator(Facade, appConfiguration, QueryService, MessagePanel);
		Coordinator.Initialize();
		Grid = new RecipeGridViewModel(Coordinator, ConfigRegistry, MessagePanel);
		Grid.Initialize();
	}

	public Task DisposeAsync()
	{
		Grid.Dispose();
		Coordinator.Dispose();
		MessagePanel.Dispose();
		return Task.CompletedTask;
	}
}

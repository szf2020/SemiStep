using Config.Facade;

using Core;

using Csv;

using Domain;
using Domain.Facade;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Tests.Core.Helpers;
using Tests.Helpers;

using TypesShared.Config;
using TypesShared.Domain;

using UI.Coordinator;
using UI.MessageService;

using Xunit;

namespace Tests.UI;

[Trait("Component", "UI")]
[Trait("Area", "Coordinator")]
[Trait("Category", "Integration")]
public sealed class RecipeMutationCoordinatorLoadRecipeTests
{
	[Fact]
	public async Task LoadRecipeAsync_Success_ClearsMessagePanelBeforeAddingNewReasons()
	{
		var (coordinator, panel, tempFilePath) = await BuildCoordinatorWithCsvAndSavedRecipeAsync();

		try
		{
			panel.AddError("stale error", "Test");

			await coordinator.LoadRecipeAsync(tempFilePath);

			panel.Entries.Should().BeEmpty();
		}
		finally
		{
			coordinator.Dispose();
			panel.Dispose();
			File.Delete(tempFilePath);
		}
	}

	[Fact]
	public async Task LoadRecipeAsync_Failure_LeavesPanelIntact()
	{
		var (coordinator, panel) = await BuildCoordinatorWithCsvAsync();

		try
		{
			panel.AddError("pre-existing error", "Test");

			await coordinator.LoadRecipeAsync("nonexistent/path/recipe.csv");

			panel.Entries.Should().ContainSingle(e => e.Source == "Test");
			panel.ErrorCount.Should().BeGreaterThan(0);
		}
		finally
		{
			coordinator.Dispose();
			panel.Dispose();
		}
	}

	[Fact]
	public async Task LoadRecipeAsync_Failure_DoesNotEmitSignal()
	{
		var (coordinator, panel) = await BuildCoordinatorWithCsvAsync();

		try
		{
			var signals = new List<MutationSignal>();
			using var sub = coordinator.StateChanged.Subscribe(signals.Add);

			await coordinator.LoadRecipeAsync("nonexistent/path/recipe.csv");

			signals.Should().BeEmpty();
		}
		finally
		{
			coordinator.Dispose();
			panel.Dispose();
		}
	}

	private static async Task<(RecipeMutationCoordinator Coordinator, MessagePanelViewModel Panel)> BuildCoordinatorWithCsvAsync()
	{
		var configDir = TestConfigLocator.GetConfigDirectory("WithGroups");
		var configLoadResult = await ConfigFacade.LoadAndValidateAsync(configDir);

		var services = new ServiceCollection()
			.AddSingleton(configLoadResult.Value)
			.AddRecipe()
			.AddDomain()
			.AddCsv()
			.AddSingleton<IClipboardService, StubClipboardService>()
			.AddSingleton<IS7Service, StubIs7Service>()
			.BuildServiceProvider();

		var facade = services.GetRequiredService<DomainFacade>();
		facade.Initialize();

		var configRegistry = services.GetRequiredService<ConfigRegistry>();
		var panel = new MessagePanelViewModel();
		var queryService = new RecipeQueryService(facade, configRegistry);
		var coordinator = new RecipeMutationCoordinator(facade, queryService, panel);

		return (coordinator, panel);
	}

	private static async Task<(RecipeMutationCoordinator Coordinator, MessagePanelViewModel Panel, string TempFilePath)> BuildCoordinatorWithCsvAndSavedRecipeAsync()
	{
		var (coordinator, panel) = await BuildCoordinatorWithCsvAsync();

		coordinator.AppendStep(RecipeTestDriver.WaitActionId);

		var tempFilePath = Path.Combine(Path.GetTempPath(), $"SemiStep.CoordinatorTest.{Guid.NewGuid():N}.csv");
		await coordinator.SaveRecipeAsync(tempFilePath);

		return (coordinator, panel, tempFilePath);
	}
}

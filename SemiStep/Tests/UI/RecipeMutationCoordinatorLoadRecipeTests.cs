using Avalonia.Threading;

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
	private const string TempFilePrefix = "SemiStep.CoordinatorTest";
	[Fact]
	public async Task LoadRecipeAsync_Success_ClearsMessagePanelBeforeAddingNewReasons()
	{
		var (coordinator, panel, tempFilePath) = await BuildCoordinatorWithCsvAndSavedRecipeAsync();

		try
		{
			await coordinator.LoadRecipeAsync("nonexistent/path/recipe.csv");
			Dispatcher.UIThread.RunJobs(null);

			await coordinator.LoadRecipeAsync(tempFilePath);
			Dispatcher.UIThread.RunJobs(null);

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
			Dispatcher.UIThread.RunJobs(null);

			await coordinator.LoadRecipeAsync("nonexistent/path/recipe.csv");
			Dispatcher.UIThread.RunJobs(null);

			panel.Entries.Should().ContainSingle(e => e.Source == "Test");
			panel.Entries.Should().Contain(e => e.IsStructural && e.IsError);
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

	[Fact]
	public async Task LoadRecipeAsync_Success_WithWarnings_ShowsWarningsInPanel()
	{
		var (coordinator, panel) = await BuildCoordinatorWithCsvAsync();
		var tempFilePath = Path.Combine(Path.GetTempPath(), $"{TempFilePrefix}.{Guid.NewGuid():N}.csv");

		try
		{
			// Save the default empty recipe so we have a valid CSV file with no steps.
			await coordinator.SaveRecipeAsync(tempFilePath);

			// Load it back — an empty recipe triggers a "Recipe has no steps" warning from the analyzer.
			var result = await coordinator.LoadRecipeAsync(tempFilePath);
			Dispatcher.UIThread.RunJobs(null);

			result.IsSuccess.Should().BeTrue("loading a valid CSV should succeed even when it has warnings");
			panel.Entries.Should().Contain(e => e.IsWarning,
				"the message panel must show the 'Recipe has no steps' warning after loading an empty recipe");
		}
		finally
		{
			coordinator.Dispose();
			panel.Dispose();
			File.Delete(tempFilePath);
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
			.AddSingleton<IPlcSyncService, StubPlcSyncService>()
			.BuildServiceProvider();

		var facade = services.GetRequiredService<DomainFacade>();
		facade.Initialize();

		var configRegistry = services.GetRequiredService<ConfigRegistry>();
		var panel = new MessagePanelViewModel();
		var queryService = new RecipeQueryService(facade, configRegistry);
		var appConfiguration = services.GetRequiredService<AppConfiguration>();
		var coordinator = new RecipeMutationCoordinator(facade, appConfiguration, queryService, panel);
		coordinator.Initialize();

		return (coordinator, panel);
	}

	private static async Task<(
		RecipeMutationCoordinator Coordinator,
		MessagePanelViewModel Panel,
		string TempFilePath)> BuildCoordinatorWithCsvAndSavedRecipeAsync()
	{
		var (coordinator, panel) = await BuildCoordinatorWithCsvAsync();

		coordinator.AppendStep(RecipeTestDriver.WaitActionId);

		var tempFilePath = Path.Combine(Path.GetTempPath(), $"{TempFilePrefix}.{Guid.NewGuid():N}.csv");
		await coordinator.SaveRecipeAsync(tempFilePath);

		return (coordinator, panel, tempFilePath);
	}
}

using Avalonia.Threading;

using Config;

using Domain;
using Domain.Facade;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Tests.Core.Helpers;
using Tests.Helpers;

using TypesShared.Config;

using UI.Coordinator;
using UI.MessageService;

using Xunit;

namespace Tests.UI;

[Trait("Component", "UI")]
[Trait("Area", "Coordinator")]
[Trait("Category", "Integration")]
public sealed class RecipeMutationCoordinatorTests : IAsyncLifetime
{
	private DomainFacade _facade = null!;
	private MessagePanelViewModel _panel = null!;
	private RecipeMutationCoordinator _coordinator = null!;

	public async Task InitializeAsync()
	{
		var (services, facade) = await CoreTestHelper.BuildAsync("WithGroups");
		_facade = facade;
		var configRegistry = services.GetRequiredService<ConfigRegistry>();
		_panel = new MessagePanelViewModel();
		var queryService = new RecipeQueryService(_facade, configRegistry);
		var appConfiguration = services.GetRequiredService<AppConfiguration>();
		var syncService = new StubPlcSyncService();
		_coordinator = new RecipeMutationCoordinator(_facade, appConfiguration, queryService, _panel, syncService);
		_coordinator.Initialize();
	}

	public Task DisposeAsync()
	{
		_coordinator.Dispose();
		_panel.Dispose();
		return Task.CompletedTask;
	}

	[Fact]
	public void AppendStep_EmitsStepAppendedSignal()
	{
		_facade.SetNewRecipe();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.StepAppended>()
			.Which.Index.Should().Be(0);
	}

	[Fact]
	public void AppendStep_SetsSuggestedSelection_ToLastIndex()
	{
		_facade.SetNewRecipe();

		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		var selection = _coordinator.ConsumeSuggestedSelection();

		selection.Should().Be(0);
	}

	[Fact]
	public void InsertStep_EmitsStepsInsertedSignal()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.InsertStep(0, RecipeTestDriver.WaitActionId);

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.StepsInserted>()
			.Which.Should().BeEquivalentTo(new MutationSignal.StepsInserted(0, 1));
	}

	[Fact]
	public void InsertStep_SetsSuggestedSelection_ToInsertedIndex()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();

		_coordinator.InsertStep(0, RecipeTestDriver.WaitActionId);
		var selection = _coordinator.ConsumeSuggestedSelection();

		selection.Should().Be(0);
	}

	[Fact]
	public void RemoveStep_EmitsStepRemovedSignal()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.RemoveStep(0);

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.StepRemoved>()
			.Which.RemovedIndex.Should().Be(0);
	}

	[Fact]
	public void RemoveStep_SuggestedSelection_IsNull_WhenRecipeBecomesEmpty()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();

		_coordinator.RemoveStep(0);
		var selection = _coordinator.ConsumeSuggestedSelection();

		selection.Should().BeNull();
	}

	[Fact]
	public void RemoveStep_SuggestedSelection_ClampedToLastIndex_WhenRemovingLast()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();

		_coordinator.RemoveStep(2);
		var selection = _coordinator.ConsumeSuggestedSelection();

		selection.Should().Be(1);
	}

	[Fact]
	public void RemoveSteps_EmitsStepsRemovedSignal()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.RemoveSteps(new[] { 0, 1 });

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.StepsRemoved>();
	}

	[Fact]
	public void ChangeStepAction_EmitsStepActionChangedSignal()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.ChangeStepAction(0, RecipeTestDriver.ForLoopActionId);

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.StepActionChanged>()
			.Which.StepIndex.Should().Be(0);
	}

	[Fact]
	public void UpdateStepProperty_EmitsPropertyUpdatedSignal_WithCorrectStepIndex()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.UpdateStepProperty(0, RecipeTestDriver.StepDurationColumn, "5");

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.PropertyUpdated>()
			.Which.StepIndex.Should().Be(0);
	}

	[Fact]
	public void Undo_EmitsRecipeReplacedSignal()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.ConsumeSuggestedSelection();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.Undo();

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.RecipeReplaced>();
	}

	[Fact]
	public void Redo_EmitsRecipeReplacedSignal()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);
		_coordinator.Undo();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.Redo();

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.RecipeReplaced>();
	}

	[Fact]
	public void NewRecipe_EmitsRecipeReplacedSignal()
	{
		_facade.SetNewRecipe();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.NewRecipe();

		signals.Should().ContainSingle().Which.Should().BeOfType<MutationSignal.RecipeReplaced>();
	}

	[Fact]
	public void NewRecipe_ClearsMessagePanel()
	{
		_facade.SetNewRecipe();
		_panel.AddError("some error", "test");
		Dispatcher.UIThread.RunJobs(null);

		_coordinator.NewRecipe();
		Dispatcher.UIThread.RunJobs(null);

		_panel.Entries.Should().BeEmpty();
	}

	[Fact]
	public void ConsumeSuggestedSelection_ReturnsValueOnce_ThenNull()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);

		var first = _coordinator.ConsumeSuggestedSelection();
		var second = _coordinator.ConsumeSuggestedSelection();

		first.Should().NotBeNull();
		second.Should().BeNull();
	}

	[Fact]
	public void AppendStep_Failure_DoesNotEmitSignal()
	{
		_facade.SetNewRecipe();
		var signals = new List<MutationSignal>();
		using var sub = _coordinator.StateChanged.Subscribe(signals.Add);

		_coordinator.AppendStep(9999);

		signals.Should().BeEmpty();
	}

	[Fact]
	public void AppendStep_Failure_AddsErrorToMessagePanel()
	{
		_facade.SetNewRecipe();

		_coordinator.AppendStep(9999);
		Dispatcher.UIThread.RunJobs(null);

		_panel.ErrorCount.Should().BeGreaterThan(0);
	}

	[Fact]
	public void IsDirty_True_AfterMutation()
	{
		_facade.SetNewRecipe();

		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);

		_coordinator.IsDirty.Should().BeTrue();
	}

	[Fact]
	public void CanUndo_True_AfterMutation()
	{
		_facade.SetNewRecipe();

		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);

		_coordinator.CanUndo.Should().BeTrue();
	}

	[Fact]
	public void CanRedo_True_AfterUndoingMutation()
	{
		_facade.SetNewRecipe();
		_coordinator.AppendStep(RecipeTestDriver.WaitActionId);

		_coordinator.Undo();

		_coordinator.CanRedo.Should().BeTrue();
	}

}

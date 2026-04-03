using FluentAssertions;

using Tests.Core.Helpers;

using Xunit;

namespace Tests.Core.Integration.Snapshot;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Snapshot")]
public sealed class CoreSnapshotStateTests(CoreFixture fixture) : IClassFixture<CoreFixture>
{
	[Fact]
	public void RejectedMutation_LeavesRecipeAndValidStateUnchanged()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.AddWait(5f).AddWait(10f);
		driver.AddFor(1).AddFor(1).AddFor(1).AddFor(1).AddWait(1f);

		driver.IsValid.Should().BeTrue("unclosed For loops produce warnings, not errors");

		var stepCountBeforeRejection = fixture.Facade.CurrentRecipe.StepCount;
		var lastValidStepCountBeforeRejection = fixture.Facade.LastValidRecipe.StepCount;

		var result = fixture.Facade.AppendStep(RecipeTestDriver.EndForLoopActionId);

		result.IsFailed.Should().BeTrue("closing a 4th nested loop exceeds the maximum nesting depth");
		driver.IsValid.Should().BeTrue("mutation was rejected, state is unchanged");
		fixture.Facade.CurrentRecipe.StepCount.Should().Be(stepCountBeforeRejection, "rejected mutation must not change the recipe");
		fixture.Facade.LastValidRecipe.StepCount.Should().Be(lastValidStepCountBeforeRejection, "last valid recipe must not change when mutation is rejected");
	}

	[Fact]
	public void LastValidRecipe_UpdatesAfterFix()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.AddFor(3).AddWait(1f);
		driver.IsValid.Should().BeTrue("unclosed For produces a warning, not an error");

		driver.AddEndFor();
		driver.IsValid.Should().BeTrue("adding EndFor closes the loop");

		fixture.Facade.LastValidRecipe.StepCount.Should().Be(3);
	}
}

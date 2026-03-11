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
	public void LastValidRecipe_PreservedAcrossInvalidTransition()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(5f).AddWait(10f);

		driver.IsValid.Should().BeTrue();
		var validStepCount = fixture.Facade.LastValidRecipe.StepCount;
		validStepCount.Should().Be(2);

		driver.AddEndFor();
		driver.IsValid.Should().BeFalse("orphan EndFor makes the recipe invalid");

		fixture.Facade.LastValidRecipe.StepCount.Should().Be(validStepCount);
	}

	[Fact]
	public void LastValidRecipe_UpdatesAfterFix()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.AddFor(3).AddWait(1f);
		driver.IsValid.Should().BeFalse("unclosed For makes the recipe invalid");

		driver.AddEndFor();
		driver.IsValid.Should().BeTrue("adding EndFor closes the loop and restores validity");

		fixture.Facade.LastValidRecipe.StepCount.Should().Be(3);
	}
}

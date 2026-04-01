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
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(5f).AddWait(10f);
		driver.AddFor(1).AddFor(1).AddFor(1).AddFor(1);
		driver.AddWait(1f);

		driver.IsValid.Should().BeTrue("unclosed For loops produce warnings, not validation errors");
		var validStepCount = fixture.Facade.LastValidRecipe.StepCount;

		driver.AddEndFor();

		driver.IsValid.Should().BeFalse("closing the 4th nested loop exceeds the maximum nesting depth");
		fixture.Facade.LastValidRecipe.StepCount.Should().Be(validStepCount);
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

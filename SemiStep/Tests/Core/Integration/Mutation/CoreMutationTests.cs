using FluentAssertions;

using Tests.Core.Helpers;

using Xunit;

namespace Tests.Core.Integration.Mutation;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Mutation")]
public sealed class CoreMutationTests(CoreFixture fixture) : IClassFixture<CoreFixture>
{
	private const float DefaultWaitDurationSeconds = 10f;

	[Fact]
	public void AppendStep_CreatesStepWithDefaults()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		driver.StepCount.Should().Be(1);
		driver.IsValid.Should().BeTrue();
	}

	[Fact]
	public void AppendStep_MultipleSteps_IncreasesCount()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait().AddWait().AddWait();

		driver.StepCount.Should().Be(3);
		driver.IsValid.Should().BeTrue();
	}

	[Fact]
	public void InsertStep_AtBeginning_ShiftsExistingSteps()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(5f).AddWait(10f);

		driver.InsertWait(0, 15f);

		driver.StepCount.Should().Be(3);
		driver.Snapshot.StepStartTimes[0].Should().Be(TimeSpan.Zero);
		driver.Snapshot.StepStartTimes[1].Should().Be(TimeSpan.FromSeconds(15));
		driver.Snapshot.StepStartTimes[2].Should().Be(TimeSpan.FromSeconds(20));
	}

	[Fact]
	public void InsertStep_InMiddle_ShiftsStartTimes()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(10f).AddWait(10f);

		var beforeSecond = driver.Snapshot.StepStartTimes[1];
		beforeSecond.Should().Be(TimeSpan.FromSeconds(10));

		driver.InsertWait(1, 5f);

		driver.StepCount.Should().Be(3);
		driver.Snapshot.StepStartTimes[2].Should().Be(TimeSpan.FromSeconds(15));
	}

	[Fact]
	public void RemoveStep_RecalculatesStartTimes()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(10f).AddWait(10f).AddWait(10f);

		driver.Snapshot.StepStartTimes[2].Should().Be(TimeSpan.FromSeconds(20));

		driver.RemoveStep(1);

		driver.StepCount.Should().Be(2);
		driver.Snapshot.StepStartTimes[1].Should().Be(TimeSpan.FromSeconds(10));
	}

	[Fact]
	public void RemoveStep_LastStep_LeavesEmptyRecipe()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		driver.StepCount.Should().Be(1);

		driver.RemoveStep(0);

		driver.StepCount.Should().Be(0);
	}

	[Fact]
	public void ReplaceAction_LongLastingToImmediate_RemovesDurationEffect()
	{
		fixture.Facade.NewRecipe();

		const float CustomDuration = 12f;

		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(CustomDuration);

		var before = driver.Snapshot.TotalDuration;
		before.Should().Be(TimeSpan.FromSeconds(CustomDuration));

		driver.ReplaceAction(0, RecipeTestDriver.ForLoopActionId);

		var after = driver.Snapshot.TotalDuration;
		after.Should().Be(TimeSpan.Zero);
	}

	[Fact]
	public void ReplaceAction_ImmediateToLongLasting_AddsDuration()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddPause();

		driver.Snapshot.TotalDuration.Should().Be(TimeSpan.Zero);

		driver.ReplaceAction(0, RecipeTestDriver.WaitActionId);
		driver.SetDuration(0, 15f);

		driver.Snapshot.TotalDuration.Should().Be(TimeSpan.FromSeconds(15));
	}

	[Fact]
	public void UpdateProperty_ChangesDuration()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(10f);

		driver.Snapshot.TotalDuration.Should().Be(TimeSpan.FromSeconds(10));

		driver.SetDuration(0, 25f);

		driver.Snapshot.TotalDuration.Should().Be(TimeSpan.FromSeconds(25));
	}

	[Fact]
	public void UpdateProperty_InvalidIndex_Throws()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		var act = () => driver.SetDuration(5, 10f);

		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[Fact]
	public void NewRecipe_ResetsToEmpty()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait().AddWait().AddWait();

		driver.StepCount.Should().Be(3);

		driver.NewRecipe();

		driver.StepCount.Should().Be(0);
	}
}

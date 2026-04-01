using FluentAssertions;

using Tests.Core.Helpers;

using Xunit;

namespace Tests.Core.Integration.Mutation;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "MutationEdgeCases")]
public sealed class CoreMutationEdgeCasesTests(CoreFixture fixture) : IClassFixture<CoreFixture>
{
	private const int InvalidNegativeIndex = -1;
	private const int InvalidLargeIndex = 100;

	[Fact]
	public void RemoveStep_NegativeIndex_Fails()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var result = fixture.Facade.RemoveStep(InvalidNegativeIndex);

		result.IsFailed.Should().BeTrue();
		driver.StepCount.Should().Be(1, "state should not change on failed mutation");
	}

	[Fact]
	public void RemoveStep_IndexBeyondCount_Fails()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var result = fixture.Facade.RemoveStep(InvalidLargeIndex);

		result.IsFailed.Should().BeTrue();
		driver.StepCount.Should().Be(1, "state should not change on failed mutation");
	}

	[Fact]
	public void UpdateProperty_NonExistentColumn_Fails()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var result = fixture.Facade.UpdateStepProperty(0, "non_existent_column", "123");

		result.IsFailed.Should().BeTrue("column key is not defined in the action's column list");
	}

	[Fact]
	public void UpdateProperty_TypeMismatch_Fails()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var result = fixture.Facade.UpdateStepProperty(0, RecipeTestDriver.StepDurationColumn, "not_a_valid_number");

		result.IsFailed.Should().BeTrue("value cannot be parsed as the column's declared property type");
	}
}

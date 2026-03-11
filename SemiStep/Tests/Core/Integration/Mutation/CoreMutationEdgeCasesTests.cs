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
	public void RemoveStep_NegativeIndex_Throws()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var act = () => fixture.Facade.RemoveStep(InvalidNegativeIndex);

		act.Should().Throw<IndexOutOfRangeException>();
	}

	[Fact]
	public void RemoveStep_IndexBeyondCount_Throws()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var act = () => fixture.Facade.RemoveStep(InvalidLargeIndex);

		act.Should().Throw<IndexOutOfRangeException>();
	}

	[Fact]
	public void UpdateProperty_NonExistentColumn_Throws()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var act = () => fixture.Facade.UpdateStepProperty(0, "non_existent_column", "123");

		act.Should().Throw<KeyNotFoundException>("column key is not defined in the action's column list");
	}

	[Fact]
	public void UpdateProperty_TypeMismatch_Throws()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait();

		var act = () => fixture.Facade.UpdateStepProperty(0, RecipeTestDriver.StepDurationColumn, "not_a_valid_number");

		act.Should().Throw<ArgumentException>("value cannot be parsed as the column's declared property type");
	}
}

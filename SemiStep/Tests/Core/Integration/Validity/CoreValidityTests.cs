using FluentAssertions;

using Tests.Core.Helpers;

using Xunit;

namespace Tests.Core.Integration.Validity;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Validity")]
public sealed class CoreValidityTests(CoreFixture fixture) : IClassFixture<CoreFixture>
{
	[Fact]
	public void EmptyRecipe_IsValid_ButHasWarning()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.IsValid.Should().BeTrue();
		driver.Snapshot.Warnings.Should().NotBeEmpty();
		driver.Snapshot.Warnings.Should().ContainSingle(w => w.Contains("empty", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ValidRecipe_NoErrors()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(10f).AddWait(20f);

		driver.IsValid.Should().BeTrue();
		driver.Snapshot.Errors.Should().BeEmpty();
	}

	[Fact]
	public void RecipeWithClosedLoop_IsValid()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddFor(3).AddWait(10f).AddEndFor();

		driver.IsValid.Should().BeTrue();
		driver.Snapshot.Errors.Should().BeEmpty();
	}

	[Fact]
	public void BrokenLoop_HasLoopIntegrityError()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddFor(3).AddWait(10f);

		driver.IsValid.Should().BeFalse();
		driver.Snapshot.Errors.Should().ContainSingle(e => e.Contains("Unclosed For loop", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void MaxDepth3Exceeded_HasLoopNestingDepthError()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.AddFor(1).AddFor(1).AddFor(1).AddFor(1);
		driver.AddWait(1f);
		driver.AddEndFor().AddEndFor().AddEndFor().AddEndFor();

		driver.IsValid.Should().BeFalse();
		driver.Snapshot.Errors.Should().ContainSingle(e => e.Contains("nesting depth", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void IsValid_DerivedFromReasons()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(10f);

		driver.Snapshot.Errors.Should().BeEmpty();
		driver.Snapshot.IsValid.Should().BeTrue("IsValid should be true when Errors is empty");

		driver.AddFor(2);

		driver.Snapshot.Errors.Should().NotBeEmpty();
		driver.Snapshot.IsValid.Should().BeFalse("IsValid should be false when Errors is non-empty");
	}

	[Fact]
	public void WarningsDoNotAffectValidity()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.Snapshot.Warnings.Should().NotBeEmpty();
		driver.IsValid.Should().BeTrue("warnings alone should not invalidate the recipe");
	}

	[Fact]
	public void MultipleErrors_AllCaptured()
	{
		fixture.Facade.NewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.AddFor(1).AddFor(1).AddWait(5f);

		driver.IsValid.Should().BeFalse();
		driver.Snapshot.Errors.Should().HaveCountGreaterThanOrEqualTo(2);
	}
}

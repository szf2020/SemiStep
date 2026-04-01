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
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.IsValid.Should().BeTrue();
		driver.Warnings.Should().NotBeEmpty();
		driver.Warnings.Should().ContainSingle(w => w.Contains("no steps", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void ValidRecipe_NoErrors()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(10f).AddWait(20f);

		driver.IsValid.Should().BeTrue();
		driver.Errors.Should().BeEmpty();
	}

	[Fact]
	public void RecipeWithClosedLoop_IsValid()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddFor(3).AddWait(10f).AddEndFor();

		driver.IsValid.Should().BeTrue();
		driver.Errors.Should().BeEmpty();
	}

	[Fact]
	public void UnclosedLoop_ProducesWarning()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddFor(3).AddWait(10f);

		driver.IsValid.Should().BeTrue("unclosed loops are warnings, not errors");
		driver.Warnings.Should().ContainSingle(w => w.Contains("Unclosed For loop", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void MaxDepth3Exceeded_HasError()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.AddFor(1).AddFor(1).AddFor(1).AddFor(1);
		driver.AddWait(1f);
		driver.AddEndFor().AddEndFor().AddEndFor().AddEndFor();

		driver.IsValid.Should().BeFalse();
		driver.Errors.Should().ContainSingle(e => e.Message.Contains("nesting depth", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void IsValid_FalseWhenErrorsPresent()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);
		driver.AddWait(10f);

		driver.Errors.Should().BeEmpty();
		driver.IsValid.Should().BeTrue();

		driver.AddFor(1).AddFor(1).AddFor(1).AddFor(1)
			.AddWait(1f)
			.AddEndFor().AddEndFor().AddEndFor().AddEndFor();

		driver.Errors.Should().NotBeEmpty();
		driver.IsValid.Should().BeFalse("exceeding max loop depth produces an error");
	}

	[Fact]
	public void WarningsDoNotAffectValidity()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.Warnings.Should().NotBeEmpty();
		driver.IsValid.Should().BeTrue("warnings alone should not invalidate the recipe");
	}

	[Fact]
	public void MultipleWarnings_AllCaptured()
	{
		fixture.Facade.SetNewRecipe();
		var driver = new RecipeTestDriver(fixture.Facade);

		driver.AddFor(1).AddFor(1).AddWait(5f);

		driver.Warnings.Should().HaveCountGreaterThanOrEqualTo(2,
			"two unclosed For loops should produce at least two warnings");
	}
}

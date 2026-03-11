using FluentAssertions;

using Tests.Config.Helpers;

using Xunit;

namespace Tests.Config.Integration.Errors;

[Trait("Category", "Integration")]
[Trait("Component", "Config")]
[Trait("Area", "ActionValidation")]
public sealed class ActionErrorTests
{
	[Fact]
	public async Task DuplicateActionId_HasError()
	{
		// Act
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DuplicateActionId");

		// Assert
		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("Duplicate action Id", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task DuplicateActionId_IdentifiesDuplicateId()
	{
		// Act
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DuplicateActionId");

		// Assert
		context.Errors.Should().Contain(e =>
				e.Contains("10"),
			"error should identify '10' as the duplicate action Id");
	}

	[Fact]
	public async Task InvalidDeployDuration_HasError()
	{
		// Act
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("InvalidDeployDuration");

		// Assert
		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("DeployDuration must be", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("immediate", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task InvalidDeployDuration_ShowsInvalidValue()
	{
		// Act
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("InvalidDeployDuration");

		// Assert
		context.Errors.Should().Contain(e =>
				e.Contains("invalid", StringComparison.OrdinalIgnoreCase),
			"error should show 'invalid' as the invalid value");
	}

	[Theory]
	[InlineData("MissingUiName", "UiName is required")]
	[InlineData("MissingDeployDuration", "DeployDuration is required")]
	[InlineData("MissingColumnKey", "column Key is required")]
	[InlineData("MissingColumnPropertyTypeId", "PropertyTypeId is required")]
	[InlineData("ActionWithZeroId", "Id must be positive")]
	[InlineData("ActionWithNegativeId", "Id must be positive")]
	public async Task StandaloneCase_HasExpectedError(string caseName, string expectedSubstring)
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync(caseName);

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains(expectedSubstring, StringComparison.OrdinalIgnoreCase));
	}
}

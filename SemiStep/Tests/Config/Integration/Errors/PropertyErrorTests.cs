using FluentAssertions;

using Tests.Config.Helpers;

using Xunit;

namespace Tests.Config.Integration.Errors;

[Trait("Category", "Integration")]
[Trait("Component", "Config")]
[Trait("Feature", "PropertyValidation")]
public class PropertyErrorTests
{
	[Fact]
	public async Task DuplicatePropertyId_HasError()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DuplicatePropertyId");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Message.Contains("Duplicate PropertyTypeId", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task DuplicatePropertyId_IdentifiesDuplicateId()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DuplicatePropertyId");

		context.Errors.Should().Contain(e =>
				e.Message.Contains("int", StringComparison.OrdinalIgnoreCase),
			"error should identify 'int' as the duplicate PropertyTypeId");
	}

	[Fact]
	public async Task InvalidSystemType_HasError()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("InvalidSystemType");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Message.Contains("SystemType must be one of", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task InvalidSystemType_ShowsInvalidValue()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("InvalidSystemType");

		context.Errors.Should().Contain(e =>
				e.Message.Contains("boolean", StringComparison.OrdinalIgnoreCase),
			"error should show 'boolean' as the invalid value");
	}

	[Fact]
	public async Task MinGreaterThanMax_HasError()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("MinGreaterThanMax");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Message.Contains("Min", StringComparison.OrdinalIgnoreCase) &&
			e.Message.Contains("Max", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task MinGreaterThanMax_ShowsValues()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("MinGreaterThanMax");

		context.Errors.Should().Contain(e =>
				e.Message.Contains("100") && e.Message.Contains("10"),
			"error should show the actual Min (100) and Max (10) values");
	}

	[Fact]
	public async Task PropertyWithMissingSystemType_HasError()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("MissingSystemType");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Message.Contains("SystemType is required", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task PropertyWithMissingFormatKind_HasError()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("MissingFormatKind");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Message.Contains("FormatKind is required", StringComparison.OrdinalIgnoreCase));
	}
}

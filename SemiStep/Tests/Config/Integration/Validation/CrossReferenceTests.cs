using FluentAssertions;

using Tests.Config.Helpers;

using Xunit;

namespace Tests.Config.Integration.Validation;

[Trait("Category", "Integration")]
[Trait("Component", "Config")]
[Trait("Area", "CrossReferenceValidation")]
public sealed class CrossReferenceTests
{
	[Theory]
	[InlineData("MissingPropertyReference", "unknown property_type_id", "nonexistent_property")]
	[InlineData("MissingColumnReference", "unknown column", "nonexistent_column")]
	[InlineData("MissingGroupReference", "unknown group_name", "nonexistent_group")]
	[InlineData("MissingActionPropertyReference", "unknown property_type_id", "nonexistent_property")]
	public async Task InvalidReference_HasErrorIdentifyingUnknownId(
		string caseName, string expectedErrorSubstring, string expectedUnknownId)
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync(caseName);

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains(expectedErrorSubstring, StringComparison.OrdinalIgnoreCase));
		context.Errors.Should().Contain(e =>
			e.Contains(expectedUnknownId, StringComparison.OrdinalIgnoreCase),
			$"error should identify '{expectedUnknownId}' as the unknown reference");
	}

	[Fact]
	public async Task ValidCrossReferences_NoErrors()
	{
		var config = await ConfigTestHelper.LoadValidCaseAsync();

		config.Should().NotBeNull();
		config.Actions.Should().NotBeEmpty("valid config should have actions");
		config.Columns.Should().NotBeEmpty("valid config should have columns");
		config.Properties.Should().NotBeEmpty("valid config should have properties");
	}

	[Fact]
	public async Task ErrorLocation_IncludesColumnKey()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("MissingPropertyReference");

		context.Errors.Should().Contain(e =>
				e.Contains("step_duration"),
			"error should reference the column 'step_duration' that has the broken reference");
	}

	[Fact]
	public async Task ErrorLocation_IncludesActionInfo()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("MissingColumnReference");

		context.Errors.Should().Contain(e =>
				e.Contains("Wait") || e.Contains("10"),
			"error should reference the action 'Wait' (Id=10) that has the broken reference");
	}
}

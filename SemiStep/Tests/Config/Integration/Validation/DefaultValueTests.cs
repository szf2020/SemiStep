using FluentAssertions;

using Tests.Config.Helpers;
using Tests.Helpers;

using Xunit;

namespace Tests.Config.Integration.Validation;

[Trait("Category", "Integration")]
[Trait("Component", "Config")]
[Trait("Area", "DefaultValueValidation")]
public sealed class DefaultValueTests
{
	[Fact]
	public async Task StringDefaultValue_ExceedsMaxLength_HasError()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DefaultValueStringTooLong");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("max_length", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("too long", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task DefaultValue_NotParsable_HasError()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DefaultValueNotParsable");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("parse", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("invalid", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task FloatDefaultValue_OutOfRange_HasError()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DefaultValueFloatOutOfRange");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("out of range", StringComparison.OrdinalIgnoreCase) ||
			e.Contains("exceeds", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task DefaultValue_OnReadOnlyColumn_HasWarning()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("DefaultValueReadOnlyConflict");

		context.HasErrors.Should().BeFalse("a default value on a readonly column is a warning, not an error");
		context.HasWarnings.Should().BeTrue("readonly column with a default value should produce a warning");
		context.Warnings.Should().Contain(w =>
			w.Contains("read_only", StringComparison.OrdinalIgnoreCase) ||
			w.Contains("readonly", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task FloatDefaultValue_ParsedWithInvariantCulture_UnderRuRu_Succeeds()
	{
		// Standalone config uses default_value "10.5" and "1.25" (dot-decimal).
		// Under ru-RU culture the decimal separator is comma, so this would fail
		// if the code incorrectly used CurrentCulture for float parsing.
		using var _ = new CultureScope("ru-RU");

		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("FloatCultureInvariant");

		context.HasErrors.Should().BeFalse(
			"dot-decimal float values should parse correctly under ru-RU culture via InvariantCulture");
		context.Configuration.Should().NotBeNull();
		context.Configuration!.Actions.Should().NotBeEmpty();
	}
}

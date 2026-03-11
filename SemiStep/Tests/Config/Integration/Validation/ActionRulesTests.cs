using FluentAssertions;

using Tests.Config.Helpers;

using Xunit;

namespace Tests.Config.Integration.Validation;

[Trait("Category", "Integration")]
[Trait("Component", "Config")]
[Trait("Area", "ActionRulesValidation")]
public sealed class ActionRulesTests
{
	[Fact]
	public async Task EnumWithoutGroupName_HasError()
	{
		var context = await ConfigTestHelper.LoadInvalidCaseAsync("EnumWithoutGroupName");

		context.HasErrors.Should().BeTrue();
		context.Errors.Should().Contain(e =>
			e.Contains("group_name", StringComparison.OrdinalIgnoreCase));
	}
}

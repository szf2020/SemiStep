using FluentAssertions;

using Tests.Config.Helpers;

using Xunit;

namespace Tests.Config.Integration.Loading;

[Trait("Category", "Integration")]
[Trait("Component", "Config")]
[Trait("Area", "ConfigNoise")]
public sealed class ConfigNoiseTests
{
	[Fact]
	public async Task LoadConfig_WithUnknownYamlFields_Succeeds()
	{
		var context = await ConfigTestHelper.LoadStandaloneCaseAsync("UnknownYamlFields");

		context.HasErrors.Should().BeFalse(
			"unknown YAML fields should be silently ignored, not cause errors");
		context.Configuration.Should().NotBeNull();
		context.Configuration!.Actions.Should().NotBeEmpty();
		context.Configuration.Properties.Should().NotBeEmpty();
	}
}

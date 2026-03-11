using FluentAssertions;

using Shared.Config.Contracts;

using Tests.Core.Helpers;

using Xunit;

namespace Tests.Core.Integration.Targets;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "TargetEdgeCases")]
public sealed class CoreTargetsEdgeCasesTests(CoreFixture fixture) : IClassFixture<CoreFixture>
{
	[Fact]
	public void GetActionByName_EmptyName_Throws()
	{
		var actionRegistry = (IActionRegistry)fixture.Services.GetService(typeof(IActionRegistry))!;

		var act = () => actionRegistry.GetActionByName("");

		act.Should().Throw<KeyNotFoundException>("empty string does not match any registered action name");
	}

	[Fact]
	public void GetGroup_InvalidId_Throws()
	{
		var groupRegistry = (IGroupRegistry)fixture.Services.GetService(typeof(IGroupRegistry))!;

		var act = () => groupRegistry.GetGroup("nonexistent");

		act.Should().Throw<KeyNotFoundException>("no group is registered with the given ID");
	}
}

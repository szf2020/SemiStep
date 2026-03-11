using FluentAssertions;

using Shared.Config.Contracts;

using Tests.Core.Helpers;

using Xunit;

namespace Tests.Core.Integration.Targets;

[Trait("Category", "Integration")]
[Trait("Component", "Core")]
[Trait("Area", "Targets")]
public sealed class CoreTargetsTests(CoreFixture fixture) : IClassFixture<CoreFixture>
{
	[Fact]
	public void Actions_List_NotEmpty()
	{
		var actionRegistry = (IActionRegistry)fixture.Services.GetService(typeof(IActionRegistry))!;
		var actions = actionRegistry.GetAll();

		actions.Should().NotBeEmpty("Standard config defines at least 4 actions");
	}

	[Fact]
	public void EnumOptions_ForGroupColumn_Succeeds()
	{
		var groupRegistry = (IGroupRegistry)fixture.Services.GetService(typeof(IGroupRegistry))!;
		var group = groupRegistry.GetGroup("valve");

		group.Items.Should().NotBeEmpty("WithGroups config defines a valve group with items");
	}

	[Fact]
	public void GroupExists_ForDefinedGroup_ReturnsTrue()
	{
		var groupRegistry = (IGroupRegistry)fixture.Services.GetService(typeof(IGroupRegistry))!;
		var exists = groupRegistry.GroupExists("valve");

		exists.Should().BeTrue("valve group is defined in WithGroups config");
	}
}

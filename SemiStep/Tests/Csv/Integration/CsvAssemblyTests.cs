using System.Collections.Immutable;

using FluentAssertions;

using Tests.Csv.Helpers;

using Xunit;

namespace Tests.Csv.Integration;

[Trait("Category", "Integration")]
[Trait("Component", "Csv")]
[Trait("Area", "CsvAssembly")]
public sealed class CsvAssemblyTests(CsvFixture fixture) : IClassFixture<CsvFixture>
{
	[Fact]
	public void Deserialize_FullyApplicableRow_NoErrors()
	{
		// Build a row where all columns are applicable to Wait action
		var csv = "action;step_duration;task;comment\n10;5;0;hello\n";
		var result = fixture.Serializer.Deserialize(csv);

		result.IsSuccess.Should().BeTrue();
		result.Recipe.Should().NotBeNull();
		result.Recipe!.StepCount.Should().Be(1);
	}

	[Fact]
	public void Deserialize_NonApplicableColumnEmpty_NoErrors()
	{
		// EndFor (action 30) does not use step_duration or task columns.
		// Those columns are left empty — should not produce errors.
		var csv = "action;step_duration;task;comment\n30;;;test comment\n";
		var result = fixture.Serializer.Deserialize(csv);

		result.IsSuccess.Should().BeTrue();
		result.Recipe.Should().NotBeNull();
		result.Recipe!.StepCount.Should().Be(1);
		result.Recipe.Steps[0].ActionKey.Should().Be(30);
	}

	[Fact]
	public void Deserialize_MixedRows_AssemblesAllSteps()
	{
		// Mix of Wait (longlasting with duration), For (with task), EndFor (minimal), Pause (minimal)
		var csv = "action;step_duration;task;comment\n" +
				  "10;5;0;wait step\n" +
				  "20;0;3;for loop\n" +
				  "10;10;0;inner wait\n" +
				  "30;;;end for\n" +
				  "40;;;pause\n";

		var result = fixture.Serializer.Deserialize(csv);

		result.IsSuccess.Should().BeTrue();
		result.Recipe.Should().NotBeNull();
		result.Recipe!.StepCount.Should().Be(5);
	}
}

using System.Collections.Immutable;

using FluentAssertions;

using Shared.Core;

using Tests.Csv.Helpers;

using Xunit;

namespace Tests.Csv.Integration;

[Trait("Component", "Csv")]
[Trait("Category", "Integration")]
[Trait("Area", "Deserialization")]
public sealed class CsvDeserializationTests(CsvFixture fixture) : IClassFixture<CsvFixture>
{
	[Fact]
	public void Deserialize_RoundTrip_PreservesRecipe()
	{
		var step = new Step(10, ImmutableDictionary<ColumnId, PropertyValue>.Empty
			.Add(new ColumnId("step_duration"), PropertyValue.FromFloat(5.0f))
			.Add(new ColumnId("comment"), PropertyValue.FromString("test comment")));

		var original = new Recipe(ImmutableList.Create(step));
		var csv = fixture.Serializer.Serialize(original);
		var result = fixture.Serializer.Deserialize(csv);

		result.IsSuccess.Should().BeTrue();
		result.Recipe.Should().NotBeNull();
		result.Recipe!.StepCount.Should().Be(1);
		result.Recipe.Steps[0].ActionKey.Should().Be(10);
	}

	[Fact]
	public void Deserialize_InvalidActionId_ReturnsError()
	{
		var csv = "action;step_duration;task;comment\n99999;10;0;test\n";
		var result = fixture.Serializer.Deserialize(csv);

		result.HasErrors.Should().BeTrue();
		result.Recipe.Should().BeNull();
	}

	[Fact]
	public void Deserialize_EmptyActionColumn_ReturnsError()
	{
		var csv = "action;step_duration;task;comment\n;10;0;test\n";
		var result = fixture.Serializer.Deserialize(csv);

		result.HasErrors.Should().BeTrue();
	}

	[Fact]
	public void Deserialize_HeaderMismatch_ReturnsError()
	{
		var csv = "wrong_header;bad_column\n10;5\n";
		var result = fixture.Serializer.Deserialize(csv);

		result.HasErrors.Should().BeTrue();
	}
}

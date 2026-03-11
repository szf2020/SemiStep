using Core.Exceptions;
using Core.Services;

using FluentAssertions;

using Shared.Core;

using Xunit;

namespace Tests.Core.Unit.Properties;

[Trait("Category", "Unit")]
[Trait("Component", "Core")]
[Trait("Area", "PropertyParsing")]
public sealed class CorePropertyParsingTests
{
	[Fact]
	public void NonNumericString_AsInt_ReturnsNull()
	{
		var result = PropertyValue.TryParse("abc", PropertyType.Int);

		result.Should().BeNull();
	}

	[Fact]
	public void String_ExceedingMaxLength_ThrowsStringTooLong()
	{
		var propertyDefinition = new PropertyDefinition(
			PropertyTypeId: "test_string",
			SystemType: "string",
			FormatKind: "numeric",
			Units: null,
			Min: null,
			Max: null,
			MaxLength: 10);

		var longString = new string('A', 11);

		var act = () => PropertyValidator.ThrowIfInvalid(propertyDefinition, longString);

		act.Should().Throw<StringTooLongException>();
	}
}

using Shared.Entities;

namespace Core.Services;

public sealed class PropertyValidator
{
	public static void ThrowIfInvalid(PropertyDefinition property, object value)
	{
		var expectedType = TryGetExpectedType(property.SystemType);

		if (expectedType == typeof(int) || expectedType == typeof(float))
		{
			var convertedValue = Convert.ChangeType(value, expectedType);
			TryValidateRange(property, convertedValue);
		}

		if (expectedType == typeof(string))
		{
			TryValidateStringLength(property, value);
		}
	}

	private static void TryValidateRange(PropertyDefinition property, object convertedValue)
	{
		if (convertedValue is not IComparable comparable)
		{
			throw new InvalidCastException(
				$"Value of type {convertedValue.GetType().Name} does not implement IComparable.");
		}

		if (property.Min.HasValue && comparable.CompareTo(property.Min.Value) < 0)
		{
			throw new ValueOutOfRangeException(
				$"Value {convertedValue} is lower than {property.Min.Value} at {property.PropertyTypeId}");
		}

		if (property.Max.HasValue && comparable.CompareTo(property.Max.Value) > 0)
		{
			throw new ValueOutOfRangeException(
				$"Value {convertedValue} is greater than {property.Max.Value} at {property.PropertyTypeId}");
		}
	}

	private static void TryValidateStringLength(PropertyDefinition property, object value)
	{
		if (value is string str && property.MaxLength.HasValue && str.Length > property.MaxLength.Value)
		{
			throw new StringTooLongException(
				$"String length of {str.Length} exceeds max applicable length {property.MaxLength.Value} at {property.PropertyTypeId}");
		}
	}

	private static Type TryGetExpectedType(string systemType)
	{
		return systemType.ToLowerInvariant() switch
		{
			"int" => typeof(int),
			"float" => typeof(float),
			"string" => typeof(string),

			_ => throw new ArgumentException(
				$"Unsupported property system type: {systemType}"),
		};
	}
}

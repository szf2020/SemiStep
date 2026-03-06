using System.Globalization;

namespace Core.Entities;

public sealed record PropertyValue
{
	internal PropertyValue(object value, PropertyType type)
	{
		Value = value;
		Type = type;
	}

	public object Value { get; init; }
	public PropertyType Type { get; init; }

	public static PropertyValue FromInt(int value)
	{
		return new PropertyValue(value, PropertyType.Int);
	}

	public static PropertyValue FromFloat(float value)
	{
		return new PropertyValue(value, PropertyType.Float);
	}

	public static PropertyValue FromString(string value)
	{
		return new PropertyValue(value, PropertyType.String);
	}

	public static PropertyValue? TryParse(string rawValue, PropertyType targetType)
	{
		return targetType switch
		{
			PropertyType.Int => int.TryParse(rawValue, out var intResult)
				? FromInt(intResult)
				: null,
			PropertyType.Float => float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatResult)
				? FromFloat(floatResult)
				: null,
			PropertyType.String => FromString(rawValue),
			_ => null
		};
	}

	public int AsInt()
	{
		return (int)Value;
	}

	public float AsFloat()
	{
		return (float)Value;
	}

	public string AsString()
	{
		return (string)Value;
	}
}

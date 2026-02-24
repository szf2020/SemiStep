namespace Core.Entities;

public sealed record PropertyValue(object Value, PropertyType Type)
{
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

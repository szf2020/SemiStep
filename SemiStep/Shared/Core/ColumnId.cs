namespace Shared.Core;

public readonly record struct ColumnId(string Value)
{
	public override string ToString()
	{
		return Value;
	}
}

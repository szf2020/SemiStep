namespace S7.Protocol;

public static class ProtocolConstants
{
	public const ushort ProtocolVersion = 1;

	public const int IntElementSize = 4;
	public const int FloatElementSize = 4;
	public const int WStringMaxChars = 32;
	public const int WStringHeaderSize = 4;
	public const int WStringElementSize = WStringHeaderSize + WStringMaxChars * 2;
}

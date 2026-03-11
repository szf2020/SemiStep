namespace Shared.Plc.Memory;

public sealed record HeaderDbLayout(
	int DbNumber,
	int MagicNumberOffset,
	int WordOrderOffset,
	int ProtocolVersionOffset,
	int ManagingDbNumberOffset,
	int IntDbNumberOffset,
	int FloatDbNumberOffset,
	int StringDbNumberOffset,
	int ExecutionDbNumberOffset,
	int TotalSize)
{
	public static HeaderDbLayout Default => new(
		DbNumber: 1,
		MagicNumberOffset: 0,
		WordOrderOffset: 2,
		ProtocolVersionOffset: 4,
		ManagingDbNumberOffset: 6,
		IntDbNumberOffset: 8,
		FloatDbNumberOffset: 10,
		StringDbNumberOffset: 12,
		ExecutionDbNumberOffset: 14,
		TotalSize: 16);
}

namespace Shared.Plc.Memory;

public sealed record ManagingDbLayout(
	int DbNumber,
	int PcStatusOffset,
	int PcTransactionIdOffset,
	int PcChecksumIntOffset,
	int PcChecksumFloatOffset,
	int PcChecksumStringOffset,
	int PcRecipeLinesOffset,
	int PlcStatusOffset,
	int PlcErrorOffset,
	int PlcStoredIdOffset,
	int PlcChecksumIntOffset,
	int PlcChecksumFloatOffset,
	int PlcChecksumStringOffset,
	int TotalSize,
	int PcDataSize)
{
	public static ManagingDbLayout Default => new(
		DbNumber: 2,
		PcStatusOffset: 0,
		PcTransactionIdOffset: 2,
		PcChecksumIntOffset: 6,
		PcChecksumFloatOffset: 10,
		PcChecksumStringOffset: 14,
		PcRecipeLinesOffset: 18,
		PlcStatusOffset: 22,
		PlcErrorOffset: 24,
		PlcStoredIdOffset: 26,
		PlcChecksumIntOffset: 30,
		PlcChecksumFloatOffset: 34,
		PlcChecksumStringOffset: 38,
		TotalSize: 42,
		PcDataSize: 22);
}

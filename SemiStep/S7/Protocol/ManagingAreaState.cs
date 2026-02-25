namespace S7.Protocol;

public sealed record ManagingAreaState(
	PcStatus PcStatus,
	uint PcTransactionId,
	uint PcChecksumInt,
	uint PcChecksumFloat,
	uint PcChecksumString,
	uint PcRecipeLines,
	PlcSyncStatus PlcStatus,
	PlcSyncError PlcError,
	uint PlcStoredId,
	uint PlcChecksumInt,
	uint PlcChecksumFloat,
	uint PlcChecksumString);

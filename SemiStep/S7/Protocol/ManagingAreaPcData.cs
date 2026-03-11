namespace S7.Protocol;

internal sealed record ManagingAreaPcData(
	PcStatus Status,
	uint TransactionId,
	uint ChecksumInt,
	uint ChecksumFloat,
	uint ChecksumString,
	uint RecipeLines);

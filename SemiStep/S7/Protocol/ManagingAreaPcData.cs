namespace S7.Protocol;

public sealed record ManagingAreaPcData(
	PcStatus Status,
	uint TransactionId,
	uint ChecksumInt,
	uint ChecksumFloat,
	uint ChecksumString,
	uint RecipeLines);

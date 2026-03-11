namespace S7.Protocol;

internal enum PlcSyncError : ushort
{
	NoError = 0,
	ChecksumMismatchInt = 1,
	ChecksumMismatchFloat = 2,
	ChecksumMismatchString = 3,
	ChecksumMismatchMultiple = 4,
	Timeout = 5
}

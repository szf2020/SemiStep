namespace S7.Protocol;

internal enum PlcSyncStatus : ushort
{
	Idle = 0,
	Busy = 1,
	CrcComputing = 2,
	Success = 3,
	Error = 4
}

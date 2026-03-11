namespace S7.Protocol;

internal enum PcStatus : ushort
{
	Idle = 0,
	Writing = 1,
	CommitRequest = 2
}

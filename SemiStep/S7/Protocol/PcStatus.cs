namespace S7.Protocol;

public enum PcStatus : ushort
{
	Idle = 0,
	Writing = 1,
	CommitRequest = 2
}

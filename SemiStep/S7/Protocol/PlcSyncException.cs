namespace S7.Protocol;

internal sealed class PlcSyncException(string message, PlcSyncError errorCode) : Exception(message)
{
	public PlcSyncError ErrorCode { get; } = errorCode;
}

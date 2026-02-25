namespace Shared.Entities;

public sealed record PlcProtocolSettings(
	int MaxRetryAttempts,
	int PollingIntervalMs,
	int WritingTimeoutMs,
	int CommitTimeoutMs)
{
	public static PlcProtocolSettings Default => new(
		MaxRetryAttempts: 3,
		PollingIntervalMs: 100,
		WritingTimeoutMs: 5000,
		CommitTimeoutMs: 5000);
}

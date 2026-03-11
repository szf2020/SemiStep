namespace S7.Sync;

internal enum SyncStatus
{
	Idle,
	Syncing,
	Synced,
	Failed,
	BlockedByExecution
}

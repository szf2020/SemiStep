using TypesShared.Core;
using TypesShared.Domain;
using TypesShared.Plc;

namespace Tests.Helpers;

public sealed class StubPlcSyncService : IPlcSyncService
{
	public PlcSyncStatus Status => PlcSyncStatus.Idle;

	public string? LastError => null;

	public DateTimeOffset? LastSyncTime => null;

	public event Action<PlcSyncStatus>? StatusChanged;

	public event Action<string?>? ErrorChanged;

	/// <summary>True if <see cref="Reset"/> was called at least once.</summary>
	public bool WasResetCalled { get; private set; }

	/// <summary>Number of times <see cref="NotifyRecipeChanged"/> was called.</summary>
	public int NotifyRecipeChangedCallCount { get; private set; }

	public void NotifyRecipeChanged(Recipe recipe, bool isValid)
	{
		NotifyRecipeChangedCallCount++;
	}

	public void Reset()
	{
		WasResetCalled = true;
	}
}

using Domain.Facade;

using FluentResults;

using TypesShared.Config;
using TypesShared.Core;
using TypesShared.Plc;

namespace UI.Coordinator;

public sealed class RecipeQueryService(
	DomainFacade domainFacade,
	ConfigRegistry configRegistry)
{
	public Recipe CurrentRecipe => domainFacade.CurrentRecipe;

	public RecipeSnapshot Snapshot => domainFacade.Snapshot.IsSuccess
		? domainFacade.Snapshot.Value
		: RecipeSnapshot.Empty;

	public bool IsDirty => domainFacade.IsDirty;
	public bool CanUndo => domainFacade.CanUndo;
	public bool CanRedo => domainFacade.CanRedo;
	public bool IsConnected => domainFacade.IsConnected;

	public IObservable<PlcExecutionInfo> ExecutionState => domainFacade.ExecutionState;
	public bool IsRecipeActive => domainFacade.IsRecipeActive;
	public PlcSyncStatus SyncStatus => domainFacade.SyncStatus;
	public string? SyncLastError => domainFacade.SyncLastError;
	public DateTimeOffset? LastSyncTime => domainFacade.LastSyncTime;
	public bool IsSyncEnabled => domainFacade.IsSyncEnabled;

	public CellState GetCellState(GridColumnDefinition column, ActionDefinition action)
	{
		return DomainFacade.GetCellState(column, action);
	}

	public int GetDefaultActionId()
	{
		return configRegistry.GetAllActions().First().Id;
	}

	public string SerializeStepsForClipboard(IReadOnlyList<Step> steps)
	{
		return domainFacade.SerializeStepsForClipboard(steps);
	}

	public Result<Recipe> DeserializeStepsFromClipboard(string csv)
	{
		return domainFacade.DeserializeStepsFromClipboard(csv);
	}
}

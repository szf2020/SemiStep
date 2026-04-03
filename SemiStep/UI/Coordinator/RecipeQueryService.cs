using Domain.Facade;

using FluentResults;

using TypesShared.Config;
using TypesShared.Core;

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

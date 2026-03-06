using Core.Analysis;
using Core.Entities;
using Core.Services;

using Domain.State;

using Shared.Entities;
using Shared.Registries;

namespace Domain.Services;

public sealed class CoreService(
	CoreFacade coreFacade,
	RecipeStateManager stateManager,
	IActionRegistry actionRegistry,
	IPropertyRegistry propertyRegistry,
	IGroupRegistry groupRegistry,
	IColumnRegistry columnRegistry)
{
	public RecipeSnapshot NewRecipe()
	{
		var recipeSnapshot = coreFacade.Analyze(Recipe.Empty);
		stateManager.Reset();
		stateManager.Update(recipeSnapshot);

		return recipeSnapshot;
	}

	public RecipeSnapshot AnalyzeRecipe(Recipe recipe)
	{
		return coreFacade.Analyze(recipe);
	}

	public RecipeSnapshot AppendStep(int actionId)
	{
		var action = actionRegistry.GetAction(actionId);
		var recipeSnapshot = coreFacade.AppendStep(stateManager.Current, action, propertyRegistry, groupRegistry);

		return recipeSnapshot;
	}

	public RecipeSnapshot InsertStep(int index, int actionId)
	{
		var action = actionRegistry.GetAction(actionId);
		var recipeSnapshot = coreFacade.InsertStep(stateManager.Current, index, action, propertyRegistry, groupRegistry);

		return recipeSnapshot;
	}

	public RecipeSnapshot ChangeStepAction(int stepIndex, int newActionId)
	{
		var newAction = actionRegistry.GetAction(newActionId);
		var recipeSnapshot = coreFacade.ChangeStepAction(stateManager.Current, stepIndex, newAction, propertyRegistry, groupRegistry);

		return recipeSnapshot;
	}

	public RecipeSnapshot RemoveStep(int index)
	{
		var recipeSnapshot = coreFacade.RemoveStep(stateManager.Current, index);

		return recipeSnapshot;
	}

	public RecipeSnapshot UpdateStepProperty(int stepIndex, string columnKey, string value)
	{
		var columnDef = columnRegistry.GetColumn(columnKey);
		var property = propertyRegistry.GetProperty(columnDef.PropertyTypeId);

		var step = stateManager.Current.Steps[stepIndex];
		var action = actionRegistry.GetAction(step.ActionKey);

		var columnId = new ColumnId(columnKey);

		var result = coreFacade.UpdateProperty(
			stateManager.Current,
			stepIndex,
			columnId,
			value,
			property,
			action,
			formulaDefinition: null);

		return result;
	}
}

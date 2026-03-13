using Shared.Config.Contracts;
using Shared.Core;

namespace Shared.ServiceContracts;

public interface ICoreService
{
	RecipeSnapshot Analyze(Recipe recipe);

	RecipeSnapshot AppendStep(
		Recipe recipe,
		ActionDefinition action,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry);

	RecipeSnapshot InsertStep(
		Recipe recipe,
		int stepIndex,
		ActionDefinition action,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry);

	RecipeSnapshot RemoveStep(Recipe recipe, int stepIndex);

	RecipeSnapshot InsertSteps(Recipe recipe, int startIndex, IReadOnlyList<Step> steps);

	RecipeSnapshot RemoveSteps(Recipe recipe, IReadOnlyList<int> indices);

	RecipeSnapshot ChangeStepAction(
		Recipe recipe,
		int stepIndex,
		ActionDefinition newAction,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry);

	RecipeSnapshot UpdateProperty(
		Recipe recipe,
		int stepIndex,
		ColumnId columnId,
		string rawValue,
		PropertyDefinition propertyDefinition,
		ActionDefinition actionDefinition);
}

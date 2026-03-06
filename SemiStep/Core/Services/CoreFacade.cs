using Core.Analysis;
using Core.Entities;
using Core.Formulas;

using Shared.Entities;
using Shared.Registries;

namespace Core.Services;

public sealed class CoreFacade(
	RecipeAnalyzer analyzer,
	FormulaApplicationCoordinator formulaCoordinator)
{
	public RecipeSnapshot Analyze(Recipe recipe)
	{
		return analyzer.Analyze(recipe);
	}

	public RecipeSnapshot AppendStep(
		Recipe recipe,
		ActionDefinition action,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry)
	{
		var newRecipe = RecipeMutator.AddStep(recipe, action, propertyRegistry, groupRegistry);

		return analyzer.Analyze(newRecipe);
	}

	public RecipeSnapshot InsertStep(
		Recipe recipe,
		int stepIndex,
		ActionDefinition action,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry)
	{
		ValidateIndexOrThrow(recipe, stepIndex);

		var newRecipe = RecipeMutator.InsertStep(recipe, stepIndex, action, propertyRegistry, groupRegistry);

		return analyzer.Analyze(newRecipe);
	}

	public RecipeSnapshot RemoveStep(Recipe recipe, int stepIndex)
	{
		ValidateIndexOrThrow(recipe, stepIndex);

		var newRecipe = RecipeMutator.RemoveStep(recipe, stepIndex);

		return analyzer.Analyze(newRecipe);
	}

	public RecipeSnapshot ChangeStepAction(
		Recipe recipe,
		int stepIndex,
		ActionDefinition newAction,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry)
	{
		ValidateIndexOrThrow(recipe, stepIndex);

		var newRecipe = RecipeMutator.ChangeStepAction(recipe, stepIndex, newAction, propertyRegistry, groupRegistry);

		return analyzer.Analyze(newRecipe);
	}

	public RecipeSnapshot UpdateProperty(
		Recipe recipe,
		int stepIndex,
		ColumnId columnId,
		string rawValue,
		PropertyDefinition propertyDefinition,
		ActionDefinition actionDefinition,
		FormulaDefinition? formulaDefinition = null)
	{
		ValidateIndexOrThrow(recipe, stepIndex);

		var propertyType = PropertyTypeMapping.FromSystemType(propertyDefinition.SystemType);
		var parsed = PropertyValue.TryParse(rawValue, propertyType)
			?? throw new ArgumentException(
				$"Cannot parse '{rawValue}' as {propertyType} for column '{columnId}'");

		PropertyValidator.ThrowIfInvalid(propertyDefinition, parsed.Value);
		var mutatedRecipe = RecipeMutator.UpdateProperty(recipe, stepIndex, columnId, parsed);

		var recalculatedStep = formulaCoordinator.ApplyIfExists(
			mutatedRecipe.Steps[stepIndex],
			actionDefinition,
			columnId,
			formulaDefinition);

		var recalculatedRecipe = mutatedRecipe with
		{
			Steps = mutatedRecipe.Steps.SetItem(stepIndex, recalculatedStep)
		};

		return analyzer.Analyze(recalculatedRecipe);
	}

	private static void ValidateIndexOrThrow(Recipe recipe, int index)
	{
		if (index < 0 || index > recipe.Steps.Count)
		{
			throw new IndexOutOfRangeException(
				$"Index {index} is out of range for recipe with {recipe.Steps.Count} steps.");
		}
	}
}

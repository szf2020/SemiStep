using Core.Formulas;

using FluentResults;

using TypesShared.Config;
using TypesShared.Core;

namespace Core.Services;

internal static class RecipeMutator
{
	public static Recipe AddStep(
		Recipe recipe,
		ActionDefinition action,
		ConfigRegistry configRegistry)
	{
		var step = StepInitializer.Create(action, configRegistry);

		return recipe with { Steps = recipe.Steps.Add(step) };
	}

	public static Recipe InsertStep(
		Recipe recipe,
		int index,
		ActionDefinition action,
		ConfigRegistry configRegistry)
	{
		var step = StepInitializer.Create(action, configRegistry);

		return recipe with { Steps = recipe.Steps.Insert(index, step) };
	}

	public static Recipe RemoveStep(
		Recipe recipe,
		int index)
	{
		return recipe with { Steps = recipe.Steps.RemoveAt(index) };
	}

	public static Recipe InsertSteps(
		Recipe recipe,
		int index,
		IReadOnlyList<Step> steps)
	{
		return recipe with { Steps = recipe.Steps.InsertRange(index, steps) };
	}

	public static Recipe RemoveSteps(
		Recipe recipe,
		IReadOnlyList<int> sortedDescendingIndices)
	{
		var steps = recipe.Steps;
		foreach (var i in sortedDescendingIndices)
		{
			steps = steps.RemoveAt(i);
		}

		return recipe with { Steps = steps };
	}

	public static Recipe UpdateProperty(
		Recipe recipe,
		int stepIndex,
		PropertyId propertyId,
		PropertyValue value)
	{
		var step = recipe.Steps[stepIndex];
		var newStep = step with { Properties = step.Properties.SetItem(propertyId, value) };

		return recipe with { Steps = recipe.Steps.SetItem(stepIndex, newStep) };
	}

	public static Result<Recipe> UpdatePropertyWithFormulas(
		Recipe recipe,
		int stepIndex,
		PropertyId propertyId,
		PropertyValue value,
		ActionDefinition action,
		FormulaApplicationCoordinator formulaCoordinator)
	{
		var mutatedRecipe = UpdateProperty(recipe, stepIndex, propertyId, value);

		var stepResult = formulaCoordinator.ApplyIfExists(
			mutatedRecipe.Steps[stepIndex],
			action,
			propertyId,
			formulaDefinition: null);

		if (stepResult.IsFailed)
		{
			return stepResult.ToResult<Recipe>();
		}

		return mutatedRecipe with
		{
			Steps = mutatedRecipe.Steps.SetItem(stepIndex, stepResult.Value)
		};
	}

	public static Recipe ChangeStepAction(
		Recipe recipe,
		int stepIndex,
		ActionDefinition newAction,
		ConfigRegistry configRegistry)
	{
		var newStep = StepInitializer.Create(newAction, configRegistry);

		return recipe with { Steps = recipe.Steps.SetItem(stepIndex, newStep) };
	}
}

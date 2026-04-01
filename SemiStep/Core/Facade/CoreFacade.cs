using Core.Analysis;
using Core.Formulas;
using Core.Services;

using FluentResults;

using TypesShared.Config;
using TypesShared.Core;
using TypesShared.Domain;

namespace Core.Facade;

internal sealed class CoreFacade(
	ConfigRegistry configRegistry,
	RecipeAnalyzer analyzer,
	FormulaApplicationCoordinator formulaCoordinator) : ICoreService
{
	public Result<RecipeSnapshot> AnalyzeRecipe(Recipe recipe)
	{
		return analyzer.Analyze(recipe);
	}

	public Result<RecipeSnapshot> AppendStep(Recipe recipe, int actionId)
	{
		var actionResult = configRegistry.GetAction(actionId);
		if (actionResult.IsFailed)
		{
			return actionResult.ToResult<RecipeSnapshot>();
		}

		var mutated = RecipeMutator.AddStep(recipe, actionResult.Value, configRegistry);
		return analyzer.Analyze(mutated);
	}

	public Result<RecipeSnapshot> InsertStep(Recipe recipe, int stepIndex, int actionId)
	{
		var indexCheck = ValidateInsertIndex(recipe, stepIndex);
		if (indexCheck.IsFailed)
		{
			return indexCheck.ToResult<RecipeSnapshot>();
		}

		var actionResult = configRegistry.GetAction(actionId);
		if (actionResult.IsFailed)
		{
			return actionResult.ToResult<RecipeSnapshot>();
		}

		var mutated = RecipeMutator.InsertStep(recipe, stepIndex, actionResult.Value, configRegistry);
		return analyzer.Analyze(mutated);
	}

	public Result<RecipeSnapshot> RemoveStep(Recipe recipe, int stepIndex)
	{
		var indexCheck = ValidateStepIndex(recipe, stepIndex);
		if (indexCheck.IsFailed)
		{
			return indexCheck.ToResult<RecipeSnapshot>();
		}

		var mutated = RecipeMutator.RemoveStep(recipe, stepIndex);
		return analyzer.Analyze(mutated);
	}

	public Result<RecipeSnapshot> InsertSteps(Recipe recipe, int startIndex, IReadOnlyList<Step> steps)
	{
		var indexCheck = ValidateInsertIndex(recipe, startIndex);
		if (indexCheck.IsFailed)
		{
			return indexCheck.ToResult<RecipeSnapshot>();
		}

		var mutated = RecipeMutator.InsertSteps(recipe, startIndex, steps);
		return analyzer.Analyze(mutated);
	}

	public Result<RecipeSnapshot> RemoveSteps(Recipe recipe, IReadOnlyList<int> indices)
	{
		foreach (var i in indices)
		{
			var indexCheck = ValidateStepIndex(recipe, i);
			if (indexCheck.IsFailed)
			{
				return indexCheck.ToResult<RecipeSnapshot>();
			}
		}

		var sorted = indices.OrderByDescending(i => i).ToList();
		var mutated = RecipeMutator.RemoveSteps(recipe, sorted);
		return analyzer.Analyze(mutated);
	}

	public Result<RecipeSnapshot> ChangeStepAction(Recipe recipe, int stepIndex, int newActionId)
	{
		var indexCheck = ValidateStepIndex(recipe, stepIndex);
		if (indexCheck.IsFailed)
		{
			return indexCheck.ToResult<RecipeSnapshot>();
		}

		var actionResult = configRegistry.GetAction(newActionId);
		if (actionResult.IsFailed)
		{
			return actionResult.ToResult<RecipeSnapshot>();
		}

		var mutated = RecipeMutator.ChangeStepAction(recipe, stepIndex, actionResult.Value, configRegistry);
		return analyzer.Analyze(mutated);
	}

	public Result<RecipeSnapshot> UpdateStepProperty(
		Recipe recipe,
		int stepIndex,
		string columnKey,
		PropertyValue value)
	{
		var indexCheck = ValidateStepIndex(recipe, stepIndex);
		if (indexCheck.IsFailed)
		{
			return indexCheck.ToResult<RecipeSnapshot>();
		}

		var step = recipe.Steps[stepIndex];
		var actionResult = configRegistry.GetAction(step.ActionKey);
		if (actionResult.IsFailed)
		{
			return actionResult.ToResult<RecipeSnapshot>();
		}

		var action = actionResult.Value;

		var validationResult = ValidatePropertyValue(action, columnKey, value);
		if (validationResult.IsFailed)
		{
			return validationResult.ToResult<RecipeSnapshot>();
		}

		return ApplyPropertyWithFormulas(recipe, stepIndex, columnKey, value, action);
	}

	private Result ValidatePropertyValue(
		ActionDefinition action,
		string columnKey,
		PropertyValue value)
	{
		var columnResult = action.FindProperty(columnKey);
		if (columnResult.IsFailed)
		{
			return columnResult.ToResult();
		}

		var actionColumn = columnResult.Value;

		var propertyResult = configRegistry.GetProperty(actionColumn.PropertyTypeId);
		if (propertyResult.IsFailed)
		{
			return propertyResult.ToResult();
		}

		var typeCheck = PropertyValidator.Validate(propertyResult.Value, value.Value);
		if (typeCheck.IsFailed)
		{
			return typeCheck;
		}

		return PropertyValidator.ValidateGroupValue(actionColumn, value, configRegistry);
	}

	private Result<RecipeSnapshot> ApplyPropertyWithFormulas(
		Recipe recipe,
		int stepIndex,
		string columnKey,
		PropertyValue value,
		ActionDefinition action)
	{
		var propertyId = new PropertyId(columnKey);

		var formulaResult = RecipeMutator.UpdatePropertyWithFormulas(
			recipe,
			stepIndex,
			propertyId,
			value,
			action,
			formulaCoordinator);

		if (formulaResult.IsFailed)
		{
			return formulaResult.ToResult<RecipeSnapshot>();
		}

		return analyzer.Analyze(formulaResult.Value);
	}

	private static Result ValidateInsertIndex(Recipe recipe, int index)
	{
		if (index < 0 || index > recipe.Steps.Count)
		{
			return Result.Fail($"Insert index {index} is out of range for recipe with {recipe.Steps.Count} steps");
		}

		return Result.Ok();
	}

	private static Result ValidateStepIndex(Recipe recipe, int index)
	{
		if (index < 0 || index >= recipe.Steps.Count)
		{
			return Result.Fail($"Step index {index} is out of range for recipe with {recipe.Steps.Count} steps");
		}

		return Result.Ok();
	}
}

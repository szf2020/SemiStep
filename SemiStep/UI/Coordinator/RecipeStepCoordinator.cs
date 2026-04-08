using System.Linq;

using Domain.Facade;

using FluentResults;

using TypesShared.Core;
using TypesShared.Domain;

namespace UI.Coordinator;

internal sealed class RecipeStepCoordinator(
	DomainFacade domainFacade,
	Func<Recipe> getCurrentRecipe,
	Action<Result> setLastRecipeResult,
	Action<int?> setSuggestedSelection,
	Action<MutationSignal> publishSignal,
	Action rebuildMessagePanel)
{
	public void AppendStep(int actionId)
	{
		var result = domainFacade.AppendStep(actionId);
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		setSuggestedSelection(getCurrentRecipe().StepCount - 1);
		publishSignal(new MutationSignal.StepAppended(getCurrentRecipe().StepCount - 1));
	}

	public void InsertStep(int index, int actionId)
	{
		var result = domainFacade.InsertStep(index, actionId);
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		setSuggestedSelection(index);
		publishSignal(new MutationSignal.StepsInserted(index, 1));
	}

	public void RemoveStep(int index)
	{
		var result = domainFacade.RemoveStep(index);
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		var currentRecipe = getCurrentRecipe();
		setSuggestedSelection(currentRecipe.StepCount > 0
			? Math.Min(index, currentRecipe.StepCount - 1)
			: null);
		publishSignal(new MutationSignal.StepRemoved(index));
	}

	public void RemoveSteps(IReadOnlyList<int> indices)
	{
		var result = domainFacade.RemoveSteps(indices);
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		var currentRecipe = getCurrentRecipe();
		setSuggestedSelection(currentRecipe.StepCount > 0
			? Math.Min(indices.Min(), currentRecipe.StepCount - 1)
			: null);
		publishSignal(new MutationSignal.StepsRemoved([.. indices]));
	}

	public void InsertSteps(int startIndex, IReadOnlyList<Step> steps)
	{
		var result = domainFacade.InsertSteps(startIndex, steps);
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		setSuggestedSelection(startIndex);
		publishSignal(new MutationSignal.StepsInserted(startIndex, steps.Count));
	}

	public void ChangeStepAction(int stepIndex, int newActionId)
	{
		var result = domainFacade.ChangeStepAction(stepIndex, newActionId);
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		setSuggestedSelection(stepIndex);
		publishSignal(new MutationSignal.StepActionChanged(stepIndex));
	}

	public void UpdateStepProperty(int stepIndex, string columnKey, string value)
	{
		var result = domainFacade.UpdateStepProperty(stepIndex, columnKey, value);
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		publishSignal(new MutationSignal.PropertyUpdated(stepIndex));
	}

	public void Undo()
	{
		var result = domainFacade.Undo();
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		setSuggestedSelection(null);
		publishSignal(new MutationSignal.RecipeReplaced());
	}

	public void Redo()
	{
		var result = domainFacade.Redo();
		setLastRecipeResult(result);
		rebuildMessagePanel();

		if (result.IsFailed)
		{
			return;
		}

		setSuggestedSelection(null);
		publishSignal(new MutationSignal.RecipeReplaced());
	}

	public void NewRecipe()
	{
		domainFacade.SetNewRecipe();
		setLastRecipeResult(Result.Ok());
		rebuildMessagePanel();
		setSuggestedSelection(null);
		publishSignal(new MutationSignal.RecipeReplaced());
	}
}

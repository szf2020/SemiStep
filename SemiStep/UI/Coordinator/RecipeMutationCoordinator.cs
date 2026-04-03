using System.Reactive.Subjects;

using Domain.Facade;

using FluentResults;

using TypesShared.Core;

using UI.MessageService;

namespace UI.Coordinator;

public sealed class RecipeMutationCoordinator(
	DomainFacade domainFacade,
	RecipeQueryService queryService,
	MessagePanelViewModel messagePanel) : IDisposable
{
	private readonly Subject<MutationSignal> _stateChanged = new();

	public IObservable<MutationSignal> StateChanged => _stateChanged;

	public int? SuggestedSelection { get; private set; }

	public Recipe CurrentRecipe => queryService.CurrentRecipe;

	public RecipeSnapshot Snapshot => queryService.Snapshot;

	public bool IsDirty => queryService.IsDirty;
	public bool CanUndo => queryService.CanUndo;
	public bool CanRedo => queryService.CanRedo;
	public bool IsConnected => queryService.IsConnected;

	public RecipeQueryService QueryService => queryService;

	public void Dispose()
	{
		_stateChanged.Dispose();
	}

	public int? ConsumeSuggestedSelection()
	{
		var value = SuggestedSelection;
		SuggestedSelection = null;

		return value;
	}

	public void AppendStep(int actionId)
	{
		var result = domainFacade.AppendStep(actionId);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = CurrentRecipe.StepCount - 1;
		_stateChanged.OnNext(new MutationSignal.StepAppended(CurrentRecipe.StepCount - 1));
	}

	public void InsertStep(int index, int actionId)
	{
		var result = domainFacade.InsertStep(index, actionId);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = index;
		_stateChanged.OnNext(new MutationSignal.StepsInserted(index, 1));
	}

	public void RemoveStep(int index)
	{
		var result = domainFacade.RemoveStep(index);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = CurrentRecipe.StepCount > 0
			? Math.Min(index, CurrentRecipe.StepCount - 1)
			: null;
		_stateChanged.OnNext(new MutationSignal.StepRemoved(index));
	}

	public void RemoveSteps(IReadOnlyList<int> indices)
	{
		var result = domainFacade.RemoveSteps(indices);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = CurrentRecipe.StepCount > 0
			? Math.Min(indices.Min(), CurrentRecipe.StepCount - 1)
			: null;
		_stateChanged.OnNext(new MutationSignal.StepsRemoved([.. indices]));
	}

	public void InsertSteps(int startIndex, IReadOnlyList<Step> steps)
	{
		var result = domainFacade.InsertSteps(startIndex, steps);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = startIndex;
		_stateChanged.OnNext(new MutationSignal.StepsInserted(startIndex, steps.Count));
	}

	public void ChangeStepAction(int stepIndex, int newActionId)
	{
		var result = domainFacade.ChangeStepAction(stepIndex, newActionId);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = stepIndex;
		_stateChanged.OnNext(new MutationSignal.StepActionChanged(stepIndex));
	}

	public void UpdateStepProperty(int stepIndex, string columnKey, string value)
	{
		var result = domainFacade.UpdateStepProperty(stepIndex, columnKey, value);
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		_stateChanged.OnNext(new MutationSignal.PropertyUpdated(stepIndex));
	}

	public void Undo()
	{
		var result = domainFacade.Undo();
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());
	}

	public void Redo()
	{
		var result = domainFacade.Redo();
		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return;
		}

		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());
	}

	public void NewRecipe()
	{
		domainFacade.SetNewRecipe();
		messagePanel.Clear();
		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());
	}

	public async Task<Result> LoadRecipeAsync(string filePath)
	{
		var result = await domainFacade.LoadRecipeAsync(filePath);

		if (!result.IsFailed)
		{
			messagePanel.Clear();
		}

		RefreshMessagePanel(result);

		if (result.IsFailed)
		{
			return result;
		}

		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.RecipeReplaced());

		return result;
	}

	public async Task SaveRecipeAsync(string filePath)
	{
		await domainFacade.SaveRecipeAsync(filePath);
		SuggestedSelection = null;
		_stateChanged.OnNext(new MutationSignal.MetadataChanged());
	}

	private void RefreshMessagePanel(Result mutationResult)
	{
		messagePanel.RefreshReasons(mutationResult.Reasons);
	}
}

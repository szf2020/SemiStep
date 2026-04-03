using System.Globalization;

using Domain.Facade;

using FluentResults;

using TypesShared.Core;
using TypesShared.Results;

namespace Tests.Core.Helpers;

public sealed class RecipeTestDriver(DomainFacade domainFacade)
{
	public RecipeSnapshot Snapshot => domainFacade.Snapshot.Value;

	public Recipe Recipe => domainFacade.CurrentRecipe;

	public bool IsValid => domainFacade.IsValid;

	public int StepCount => Recipe.StepCount;

	public IReadOnlyList<IError> Errors => domainFacade.Snapshot
		.Reasons
		.OfType<IError>()
		.ToList();

	public IReadOnlyList<string> Warnings => domainFacade.Snapshot
		.Reasons
		.OfType<Warning>()
		.Select(w => w.Message)
		.ToList();

	#region Recipe Management

	public RecipeTestDriver NewRecipe()
	{
		domainFacade.SetNewRecipe();

		return this;
	}

	#endregion

	#region Service Action IDs

	public const int WaitActionId = 10;
	public const int ForLoopActionId = 20;
	public const int EndForLoopActionId = 30;
	public const int PauseActionId = 40;
	public const int WithGroupActionId = 50;

	#endregion

	#region Column Keys

	public const string StepDurationColumn = "step_duration";
	public const string TaskColumn = "task";
	public const string CommentColumn = "comment";
	public const string TargetColumn = "target";

	#endregion

	#region Add Steps

	public RecipeTestDriver AddWait(float durationSeconds = 10f)
	{
		domainFacade.AppendStep(WaitActionId);
		var lastIndex = Recipe.StepCount - 1;
		domainFacade.UpdateStepProperty(lastIndex, StepDurationColumn, durationSeconds.ToString(CultureInfo.InvariantCulture));

		return this;
	}

	public RecipeTestDriver AddFor(int iterations)
	{
		domainFacade.AppendStep(ForLoopActionId);
		var lastIndex = Recipe.StepCount - 1;
		domainFacade.UpdateStepProperty(lastIndex, TaskColumn, ((float)iterations).ToString(CultureInfo.InvariantCulture));

		return this;
	}

	public RecipeTestDriver AddEndFor()
	{
		domainFacade.AppendStep(EndForLoopActionId);

		return this;
	}

	public RecipeTestDriver AddPause()
	{
		domainFacade.AppendStep(PauseActionId);

		return this;
	}

	public RecipeTestDriver AddStep(int actionId)
	{
		domainFacade.AppendStep(actionId);

		return this;
	}

	#endregion

	#region Insert Steps

	public RecipeTestDriver InsertWait(int index, float durationSeconds = 10f)
	{
		domainFacade.InsertStep(index, WaitActionId);
		domainFacade.UpdateStepProperty(index, StepDurationColumn, durationSeconds.ToString(CultureInfo.InvariantCulture));

		return this;
	}

	public RecipeTestDriver InsertFor(int index, int iterations)
	{
		domainFacade.InsertStep(index, ForLoopActionId);
		domainFacade.UpdateStepProperty(index, TaskColumn, ((float)iterations).ToString(CultureInfo.InvariantCulture));

		return this;
	}

	public RecipeTestDriver InsertEndFor(int index)
	{
		domainFacade.InsertStep(index, EndForLoopActionId);

		return this;
	}

	#endregion

	#region Modify Steps

	public RecipeTestDriver SetDuration(int index, float seconds)
	{
		domainFacade.UpdateStepProperty(index, StepDurationColumn, seconds.ToString(CultureInfo.InvariantCulture));

		return this;
	}

	public RecipeTestDriver SetTask(int index, float value)
	{
		domainFacade.UpdateStepProperty(index, TaskColumn, value.ToString(CultureInfo.InvariantCulture));

		return this;
	}

	public RecipeTestDriver ReplaceAction(int index, int actionId)
	{
		domainFacade.ChangeStepAction(index, actionId);

		return this;
	}

	public RecipeTestDriver RemoveStep(int index)
	{
		domainFacade.RemoveStep(index);

		return this;
	}

	public RecipeTestDriver InsertSteps(int startIndex, IReadOnlyList<Step> steps)
	{
		domainFacade.InsertSteps(startIndex, steps);

		return this;
	}

	public RecipeTestDriver RemoveSteps(IReadOnlyList<int> indices)
	{
		domainFacade.RemoveSteps(indices);

		return this;
	}

	#endregion
}

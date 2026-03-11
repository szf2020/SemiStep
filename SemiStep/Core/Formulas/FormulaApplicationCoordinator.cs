using Shared.Core;

namespace Core.Formulas;

internal sealed class FormulaApplicationCoordinator(FormulaEngine engine, StepVariableAdapter adapter)
{
	public Step ApplyIfExists(
		Step step,
		ActionDefinition action,
		ColumnId changedColumn,
		FormulaDefinition? formulaDefinition)
	{
		if (formulaDefinition is null)
		{
			return step;
		}

		if (IsFormulaNotNeeded(changedColumn, formulaDefinition))
		{
			return step;
		}

		var currentStepVariables = StepVariableAdapter.ExtractVariableNames(step, formulaDefinition.RecalcOrder);

		var calculationResult = engine.Calculate(action.Id, changedColumn.Value, currentStepVariables);

		var newStep = StepVariableAdapter.ApplyChanges(step, calculationResult);

		return newStep;
	}

	private static bool IsFormulaNotNeeded(ColumnId changedColumn, FormulaDefinition formula)
	{
		return !formula.RecalcOrder.Contains(changedColumn.Value, StringComparer.OrdinalIgnoreCase);
	}
}

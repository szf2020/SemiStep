using Core.Entities;

using Shared.Entities;

namespace Core.Formulas;

public sealed class FormulaApplicationCoordinator(IFormulaEngine engine, IStepVariableAdapter adapter)
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

		var currentStepVariables = adapter.ExtractVariableNames(step, formulaDefinition.RecalcOrder);

		var calculationResult = engine.Calculate(action.Id, changedColumn.Value, currentStepVariables);

		var newStep = adapter.ApplyChanges(step, calculationResult);

		return newStep;
	}

	private static bool IsFormulaNotNeeded(ColumnId changedColumn, FormulaDefinition formula)
	{
		return !formula.RecalcOrder.Contains(changedColumn.Value, StringComparer.OrdinalIgnoreCase);
	}
}

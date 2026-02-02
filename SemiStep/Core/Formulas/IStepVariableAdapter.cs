using Core.Entities;

namespace Core.Formulas;

public interface IStepVariableAdapter
{
	IReadOnlyDictionary<string, double> ExtractVariableNames(Step step, IReadOnlyList<string> variableNames);

	Step ApplyChanges(Step originalStep, IReadOnlyDictionary<string, double> variableUpdates);
}

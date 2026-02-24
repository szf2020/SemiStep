namespace Core.Formulas;

public interface IFormulaEngine
{
	Dictionary<string, double> Calculate(int actionId, string changedVariable,
		IReadOnlyDictionary<string, double> currentValues);
}

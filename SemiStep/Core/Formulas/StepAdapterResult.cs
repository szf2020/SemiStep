using TypesShared.Core;

namespace Core.Formulas;

internal sealed record StepAdapterResult(Step Step, IReadOnlyDictionary<string, double> Variables);

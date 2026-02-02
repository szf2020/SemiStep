using Core.Entities;

namespace Core.Formulas;

public sealed record StepAdapterResult(Step Step, IReadOnlyDictionary<string, double> Variables);

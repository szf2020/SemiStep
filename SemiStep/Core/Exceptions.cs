namespace Core;

public sealed class ValueOutOfRangeException(string message) : Exception(message);

public sealed class TypeMismatchException(string message) : Exception(message);

public sealed class StringTooLongException(string message) : Exception(message);

public sealed class FormulaVariableNotFoundException(string message) : Exception(message);

public sealed class FormulaNoTargetVariableException(string message) : Exception(message);

public sealed class FormulaComputationOverflowException(string message) : Exception(message);

public sealed class FormulaNotFoundException(string message) : Exception(message);

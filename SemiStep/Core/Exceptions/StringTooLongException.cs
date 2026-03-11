namespace Core.Exceptions;

internal sealed class StringTooLongException(string message) : Exception(message);

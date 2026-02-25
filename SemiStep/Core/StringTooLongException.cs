namespace Core;

public sealed class StringTooLongException(string message) : Exception(message);

namespace Shared.Reasons;

public sealed record PropertyValueError(string Message) : AbstractError(Message);

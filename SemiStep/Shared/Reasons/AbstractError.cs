namespace Shared.Reasons;

public abstract record AbstractError(string Message) : AbstractReason(Message);

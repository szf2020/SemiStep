namespace Shared.Reasons;

public abstract record AbstractWarning(string Message) : AbstractReason(Message);

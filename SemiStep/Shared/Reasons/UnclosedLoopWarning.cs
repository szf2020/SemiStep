namespace Shared.Reasons;

public sealed record UnclosedLoopWarning(string Message) : AbstractWarning(Message);

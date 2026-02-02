namespace Shared.Reasons;

public sealed record LoopIntegrityError(string Message) : AbstractError(Message);

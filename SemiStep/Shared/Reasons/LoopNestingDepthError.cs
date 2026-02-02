namespace Shared.Reasons;

public sealed record LoopNestingDepthError(string Message) : AbstractError(Message);

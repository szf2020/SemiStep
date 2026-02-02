namespace Shared.Reasons;

public sealed record EmptyRecipeWarning(string Message) : AbstractWarning(Message);

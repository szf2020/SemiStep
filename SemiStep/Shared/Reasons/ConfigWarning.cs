namespace Shared.Reasons;

public sealed record ConfigWarning(string Message, string? Location) : AbstractWarning(Message)
{
	public static ConfigWarning General(string message, string? location = null)
		=> new(message, location);
}

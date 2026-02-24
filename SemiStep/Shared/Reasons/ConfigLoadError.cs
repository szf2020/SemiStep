namespace Shared.Reasons;

public sealed record ConfigLoadError(string Message, string? Location) : AbstractError(Message)
{
	public static ConfigLoadError General(string message, string? location = null)
	{
		return new(message, location);
	}
}

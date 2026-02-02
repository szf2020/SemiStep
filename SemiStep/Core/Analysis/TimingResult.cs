namespace Core.Analysis;

public sealed record TimingResult(
	IReadOnlyDictionary<int, TimeSpan> StepStartTimes,
	TimeSpan TotalDuration)
{
	public static TimingResult Empty => new(new Dictionary<int, TimeSpan>(), TimeSpan.Zero);
}

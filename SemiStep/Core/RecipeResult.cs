using Core.Analysis;
using Core.Entities;

using Shared.Reasons;

namespace Core;

public sealed record RecipeResult(
	Recipe Recipe,
	LoopStructure Loops,
	TimingResult Timing,
	IReadOnlyList<AbstractReason> Reasons)
{
	public bool HasErrors => Reasons.Any(r => r is AbstractError);
	public bool HasWarnings => Reasons.Any(r => r is AbstractWarning);
	public bool CanProceed => !HasErrors;

	public IEnumerable<AbstractError> Errors => Reasons.OfType<AbstractError>();
	public IEnumerable<AbstractWarning> Warnings => Reasons.OfType<AbstractWarning>();

	public static RecipeResult Success(Recipe recipe, LoopStructure loops, TimingResult timing)
		=> new(recipe, loops, timing, []);

	public static RecipeResult WithReasons(
		Recipe recipe,
		LoopStructure loops,
		TimingResult timing,
		IReadOnlyList<AbstractReason> reasons)
		=> new(recipe, loops, timing, reasons);

	public static RecipeResult Empty => new(
		Recipe.Empty,
		LoopStructure.Empty,
		TimingResult.Empty,
		[]);
}

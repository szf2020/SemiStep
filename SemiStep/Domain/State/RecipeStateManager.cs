using FluentResults;

using TypesShared.Core;
using TypesShared.Results;

namespace Domain.State;

internal sealed class RecipeStateManager
{
	public Result<RecipeSnapshot>? LatestSnapshot { get; private set; } = Result.Ok(RecipeSnapshot.Empty);
	public Recipe Current => LatestSnapshot is { IsSuccess: true }
		? LatestSnapshot.Value.Recipe
		: Recipe.Empty;
	public Recipe LastValidRecipe { get; private set; } = Recipe.Empty;

	public bool IsDirty { get; private set; }
	public bool IsValid => LatestSnapshot is not null
		&& LatestSnapshot.IsSuccess
		&& !LatestSnapshot.Reasons.OfType<ValidationError>().Any();

	public event Action<Recipe>? RecipeChanged;

	public void Update(Result<RecipeSnapshot> snapshot)
	{
		LatestSnapshot = snapshot;
		IsDirty = true;

		if (snapshot.IsSuccess && !snapshot.Reasons.OfType<ValidationError>().Any())
		{
			LastValidRecipe = snapshot.Value.Recipe;
		}

		if (snapshot.IsSuccess)
		{
			RecipeChanged?.Invoke(snapshot.Value.Recipe);
		}
	}

	public void MarkSaved()
	{
		IsDirty = false;
	}

	public void Reset()
	{
		LatestSnapshot = RecipeSnapshot.Empty;
		LastValidRecipe = Recipe.Empty;
		IsDirty = false;
	}
}

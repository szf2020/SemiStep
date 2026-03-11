using Shared.Core;

namespace Domain.State;

internal sealed class RecipeStateManager
{
	public RecipeSnapshot? LastSnapshot { get; private set; }
	public Recipe Current => LastSnapshot?.Recipe ?? Recipe.Empty;
	public Recipe LastValidRecipe { get; private set; } = Recipe.Empty;

	public bool IsDirty { get; private set; }
	public bool IsValid => LastSnapshot?.IsValid ?? false;

	public event Action<Recipe>? RecipeChanged;

	public void Update(RecipeSnapshot snapshot)
	{
		LastSnapshot = snapshot;
		IsDirty = true;

		if (snapshot.IsValid)
		{
			LastValidRecipe = snapshot.Recipe;
			RecipeChanged?.Invoke(LastValidRecipe);
		}
	}

	public void MarkSaved()
	{
		IsDirty = false;
	}

	public void Reset()
	{
		LastSnapshot = RecipeSnapshot.Empty;
		LastValidRecipe = Recipe.Empty;
		IsDirty = false;
	}
}

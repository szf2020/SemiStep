using Core;
using Core.Entities;

namespace Domain.State;

public sealed class RecipeStateManager
{
	private Recipe _lastValid = Recipe.Empty;

	public RecipeResult? LastResult { get; private set; }
	public Recipe Current => LastResult?.Recipe ?? Recipe.Empty;
	public bool IsDirty { get; private set; }
	public bool IsValid => LastResult?.CanProceed ?? false;

	public void Update(RecipeResult result)
	{
		LastResult = result;
		IsDirty = true;

		if (result.CanProceed)
		{
			_lastValid = result.Recipe;
		}
	}

	public void Load(RecipeResult result)
	{
		LastResult = result;
		IsDirty = false;

		if (result.CanProceed)
		{
			_lastValid = result.Recipe;
		}
	}

	public void MarkSaved()
	{
		IsDirty = false;
	}

	public void Reset()
	{
		LastResult = RecipeResult.Empty;
		_lastValid = Recipe.Empty;
		IsDirty = false;
	}
}

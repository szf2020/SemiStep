namespace S7.Protocol;

internal sealed record ManagingAreaState(
	bool Committed,
	int RecipeLines);

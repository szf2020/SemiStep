namespace S7.Protocol;

public sealed record PlcExecutionState(
	bool RecipeActive,
	int ActualLine,
	float StepCurrentTime,
	int ForLoopCount1,
	int ForLoopCount2,
	int ForLoopCount3);

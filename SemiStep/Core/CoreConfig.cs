using Shared.Core;

namespace Core;

internal class CoreConfig
{
	private const string IterationColumnName = "task";

	public readonly ColumnId IterationColumnId = new(IterationColumnName);
}

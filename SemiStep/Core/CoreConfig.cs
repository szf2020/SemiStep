using TypesShared.Core;

namespace Core;

internal class CoreConfig
{
	private const string IterationColumnName = "task";

	public PropertyId IterationPropertyId { get; } = new(IterationColumnName);
}

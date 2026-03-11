namespace Shared.Config.Contracts;

public interface IColumnRegistry
{
	void Initialize(IReadOnlyDictionary<string, GridColumnDefinition> columns);
	GridColumnDefinition GetColumn(string key);
	bool ColumnExists(string key);
	IReadOnlyList<GridColumnDefinition> GetAll();
}

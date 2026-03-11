using Shared.Config;
using Shared.Config.Contracts;

namespace Domain.Registries;

internal sealed class ColumnRegistry : IColumnRegistry
{
	private readonly Dictionary<string, GridColumnDefinition> _columns = new(StringComparer.OrdinalIgnoreCase);
	private IReadOnlyList<GridColumnDefinition>? _cachedAll;

	public void Initialize(IReadOnlyDictionary<string, GridColumnDefinition> columns)
	{
		_columns.Clear();
		_cachedAll = null;

		foreach (var (key, column) in columns)
		{
			_columns[key] = column;
		}
	}

	public GridColumnDefinition GetColumn(string key)
	{
		if (!_columns.TryGetValue(key, out var column))
		{
			throw new KeyNotFoundException($"Column with key '{key}' not found");
		}

		return column;
	}

	public bool ColumnExists(string key)
	{
		return _columns.ContainsKey(key);
	}

	public IReadOnlyList<GridColumnDefinition> GetAll()
	{
		return _cachedAll ??= _columns.Values.ToList();
	}
}

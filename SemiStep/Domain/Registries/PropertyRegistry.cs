using Shared.Config.Contracts;
using Shared.Core;

namespace Domain.Registries;

internal sealed class PropertyRegistry : IPropertyRegistry
{
	private readonly Dictionary<string, PropertyDefinition> _properties = new(StringComparer.OrdinalIgnoreCase);
	private IReadOnlyList<PropertyDefinition>? _cachedAll;

	public void Initialize(IReadOnlyDictionary<string, PropertyDefinition> properties)
	{
		_properties.Clear();
		_cachedAll = null;

		foreach (var (key, property) in properties)
		{
			_properties[key] = property;
		}
	}

	public PropertyDefinition GetProperty(string propertyTypeId)
	{
		if (!_properties.TryGetValue(propertyTypeId, out var property))
		{
			throw new KeyNotFoundException($"Property with id '{propertyTypeId}' not found");
		}

		return property;
	}

	public bool PropertyExists(string propertyTypeId)
	{
		return _properties.ContainsKey(propertyTypeId);
	}

	public IReadOnlyList<PropertyDefinition> GetAll()
	{
		return _cachedAll ??= _properties.Values.ToList();
	}
}

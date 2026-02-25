using Shared.Entities;
using Shared.Registries;

namespace Domain.Registries;

public sealed class GroupRegistry : IGroupRegistry
{
	private readonly Dictionary<string, GroupDefinition> _groups = new(StringComparer.OrdinalIgnoreCase);
	private IReadOnlyList<GroupDefinition>? _cachedAll;

	public void Initialize(IReadOnlyDictionary<string, GroupDefinition> groups)
	{
		_groups.Clear();
		_cachedAll = null;

		foreach (var (key, group) in groups)
		{
			_groups[key] = group;
		}
	}

	public GroupDefinition GetGroup(string groupId)
	{
		if (!_groups.TryGetValue(groupId, out var group))
		{
			throw new KeyNotFoundException($"Group with id '{groupId}' not found");
		}

		return group;
	}

	public bool GroupExists(string groupId)
	{
		return _groups.ContainsKey(groupId);
	}

	public IReadOnlyList<GroupDefinition> GetAll()
	{
		return _cachedAll ??= _groups.Values.ToList();
	}
}

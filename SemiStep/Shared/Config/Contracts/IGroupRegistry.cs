namespace Shared.Config.Contracts;

public interface IGroupRegistry
{
	void Initialize(IReadOnlyDictionary<string, GroupDefinition> groups);
	GroupDefinition GetGroup(string groupId);
	bool GroupExists(string groupId);
	IReadOnlyList<GroupDefinition> GetAll();
}

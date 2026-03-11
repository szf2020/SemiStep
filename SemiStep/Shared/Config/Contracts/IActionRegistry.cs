using Shared.Core;

namespace Shared.Config.Contracts;

public interface IActionRegistry
{
	void Initialize(IReadOnlyDictionary<int, ActionDefinition> actions);
	ActionDefinition GetAction(int id);
	ActionDefinition GetActionByName(string name);
	bool ActionExists(int id);
	bool ActionExistsByName(string name);
	IReadOnlyList<ActionDefinition> GetAll();
}

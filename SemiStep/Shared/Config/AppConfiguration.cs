using Shared.Core;
using Shared.Plc;
using Shared.Style;

namespace Shared.Config;

public sealed record AppConfiguration(
	IReadOnlyDictionary<string, PropertyDefinition> Properties,
	IReadOnlyDictionary<string, GridColumnDefinition> Columns,
	IReadOnlyDictionary<string, GroupDefinition> Groups,
	IReadOnlyDictionary<int, ActionDefinition> Actions,
	GridStyleOptions GridStyle,
	PlcConfiguration PlcConfiguration);

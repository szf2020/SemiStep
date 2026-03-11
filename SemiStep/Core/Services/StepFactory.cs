using System.Collections.Immutable;

using Shared.Config.Contracts;
using Shared.Core;

namespace Core.Services;

internal sealed class StepFactory
{
	public static Step Create(
		ActionDefinition action,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry)
	{
		var propertyValues = action.Columns
			.ToImmutableDictionary(
				col => new ColumnId(col.Key),
				col => ResolveValue(col, propertyRegistry, groupRegistry));

		return new Step(action.Id, propertyValues);
	}

	private static PropertyValue ResolveValue(
		ActionColumnDefinition column,
		IPropertyRegistry propertyRegistry,
		IGroupRegistry groupRegistry)
	{
		var propertyDefinition = propertyRegistry.GetProperty(column.PropertyTypeId);
		var propertyType = PropertyTypeMapping.FromSystemType(propertyDefinition.SystemType);

		if (!string.IsNullOrEmpty(column.DefaultValue))
		{
			return PropertyValue.TryParse(column.DefaultValue, propertyType)
				   ?? PropertyValue.FromString(column.DefaultValue);
		}

		if (column.GroupName is not null && groupRegistry.GroupExists(column.GroupName))
		{
			var group = groupRegistry.GetGroup(column.GroupName);
			if (group.Items.Count > 0)
			{
				var firstKey = group.Items.Keys.Min();

				return PropertyValue.FromInt(firstKey);
			}
		}

		return GetDefaultPropertyValue(propertyType);
	}

	private static PropertyValue GetDefaultPropertyValue(PropertyType type)
	{
		return type switch
		{
			PropertyType.Int => PropertyValue.FromInt(0),
			PropertyType.Float => PropertyValue.FromFloat(0f),
			_ => PropertyValue.FromString("")
		};
	}
}

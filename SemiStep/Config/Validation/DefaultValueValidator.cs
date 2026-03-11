using System.Globalization;

using Config.Dto;
using Config.Models;

namespace Config.Validation;

internal static class DefaultValueValidator
{
	public static ConfigContext Validate(ConfigContext context)
	{
		if (context.HasErrors)
		{
			return context;
		}

		if (context.Properties is null || context.Columns is null || context.Actions is null)
		{
			return context;
		}

		var propertyByTypeId = BuildPropertyLookup(context.Properties);
		var columnByKey = BuildColumnLookup(context.Columns);

		foreach (var action in context.Actions)
		{
			if (action.Columns is null)
			{
				continue;
			}

			foreach (var actionColumn in action.Columns)
			{
				ValidateActionColumn(context, action, actionColumn, propertyByTypeId, columnByKey);
			}
		}

		return context;
	}

	private static void ValidateActionColumn(
		ConfigContext context,
		ActionDto action,
		ActionColumnDto actionColumn,
		Dictionary<string, PropertyDto> propertyByTypeId,
		Dictionary<string, ColumnDto> columnByKey)
	{
		if (string.IsNullOrEmpty(actionColumn.DefaultValue))
		{
			return;
		}

		var location = $"actions, Id={action.Id}, column '{actionColumn.Key}'";

		ValidateReadOnlyConflict(context, actionColumn, columnByKey, location);
		ValidateValueAgainstProperty(context, actionColumn, propertyByTypeId, location);
	}

	private static void ValidateReadOnlyConflict(
		ConfigContext context,
		ActionColumnDto actionColumn,
		Dictionary<string, ColumnDto> columnByKey,
		string location)
	{
		if (string.IsNullOrEmpty(actionColumn.Key))
		{
			return;
		}

		if (!columnByKey.TryGetValue(actionColumn.Key, out var columnDto))
		{
			return;
		}

		if (columnDto.BusinessLogic?.ReadOnly == true)
		{
			context.AddWarning(
				$"Column '{actionColumn.Key}' is read_only but has a default_value defined; the default value will have no effect",
				location);
		}
	}

	private static void ValidateValueAgainstProperty(
		ConfigContext context,
		ActionColumnDto actionColumn,
		Dictionary<string, PropertyDto> propertyByTypeId,
		string location)
	{
		if (string.IsNullOrEmpty(actionColumn.PropertyTypeId))
		{
			return;
		}

		if (!propertyByTypeId.TryGetValue(actionColumn.PropertyTypeId, out var propertyDto))
		{
			return;
		}

		var systemType = propertyDto.SystemType?.ToLowerInvariant();
		var defaultValue = actionColumn.DefaultValue!;

		switch (systemType)
		{
			case "string":
				ValidateStringDefault(context, defaultValue, propertyDto, location);

				break;
			case "int":
				ValidateIntDefault(context, defaultValue, propertyDto, location);

				break;
			case "float":
				ValidateFloatDefault(context, defaultValue, propertyDto, location);

				break;
		}
	}

	private static void ValidateStringDefault(
		ConfigContext context,
		string defaultValue,
		PropertyDto propertyDto,
		string location)
	{
		if (propertyDto.MaxLength.HasValue && defaultValue.Length > propertyDto.MaxLength.Value)
		{
			context.AddError(
				$"Default value exceeds max_length ({propertyDto.MaxLength.Value}): " +
				$"value has {defaultValue.Length} characters",
				location);
		}
	}

	private static void ValidateIntDefault(
		ConfigContext context,
		string defaultValue,
		PropertyDto propertyDto,
		string location)
	{
		if (!int.TryParse(defaultValue, CultureInfo.InvariantCulture, out var parsed))
		{
			context.AddError(
				$"Cannot parse default value '{defaultValue}' as int",
				location);

			return;
		}

		ValidateNumericRange(context, parsed, propertyDto, location);
	}

	private static void ValidateFloatDefault(
		ConfigContext context,
		string defaultValue,
		PropertyDto propertyDto,
		string location)
	{
		if (!float.TryParse(defaultValue, CultureInfo.InvariantCulture, out var parsed))
		{
			context.AddError(
				$"Cannot parse default value '{defaultValue}' as float",
				location);

			return;
		}

		ValidateNumericRange(context, parsed, propertyDto, location);
	}

	private static void ValidateNumericRange(
		ConfigContext context,
		double value,
		PropertyDto propertyDto,
		string location)
	{
		if (propertyDto.Min.HasValue && value < propertyDto.Min.Value)
		{
			context.AddError(
				string.Format(
					CultureInfo.InvariantCulture,
					"Default value {0} is out of range: minimum is {1}",
					value,
					propertyDto.Min.Value),
				location);
		}

		if (propertyDto.Max.HasValue && value > propertyDto.Max.Value)
		{
			context.AddError(
				string.Format(
					CultureInfo.InvariantCulture,
					"Default value {0} is out of range: maximum is {1}",
					value,
					propertyDto.Max.Value),
				location);
		}
	}

	private static Dictionary<string, PropertyDto> BuildPropertyLookup(List<PropertyDto> properties)
	{
		var lookup = new Dictionary<string, PropertyDto>(StringComparer.OrdinalIgnoreCase);

		foreach (var property in properties)
		{
			if (!string.IsNullOrEmpty(property.PropertyTypeId))
			{
				lookup.TryAdd(property.PropertyTypeId, property);
			}
		}

		return lookup;
	}

	private static Dictionary<string, ColumnDto> BuildColumnLookup(List<ColumnDto> columns)
	{
		var lookup = new Dictionary<string, ColumnDto>(StringComparer.OrdinalIgnoreCase);

		foreach (var column in columns)
		{
			if (!string.IsNullOrEmpty(column.Key))
			{
				lookup.TryAdd(column.Key, column);
			}
		}

		return lookup;
	}
}

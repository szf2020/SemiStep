using System.Collections.Immutable;

using TypesShared.Config;
using TypesShared.Core;
using TypesShared.Plc;

namespace S7.Serialization;

internal sealed class RecipeConverter(ConfigRegistry configRegistry)
{
	public PlcRecipeData FromRecipe(Recipe recipe)
	{
		if (recipe.StepCount == 0)
		{
			return PlcRecipeData.Empty;
		}

		var intValues = new List<int>();
		var floatValues = new List<float>();
		var stringValues = new List<string>();

		foreach (var step in recipe.Steps)
		{
			intValues.Add(step.ActionKey);

			var action = configRegistry.GetAction(step.ActionKey).Value;

			foreach (var columnDef in action.Properties)
			{
				var columnId = new PropertyId(columnDef.Key);
				var propertyDef = configRegistry.GetProperty(columnDef.PropertyTypeId).Value;
				var propertyType = PropertyTypeMapping.FromSystemType(propertyDef.SystemType);

				if (!step.Properties.TryGetValue(columnId, out var propertyValue))
				{
					AppendDefaultValue(propertyType, intValues, floatValues, stringValues);

					continue;
				}

				switch (propertyType)
				{
					case PropertyType.Int:
						intValues.Add(propertyValue.AsInt());

						break;
					case PropertyType.Float:
						floatValues.Add(propertyValue.AsFloat());

						break;
					case PropertyType.String:
						stringValues.Add(propertyValue.AsString());

						break;
				}
			}
		}

		return new PlcRecipeData(
			IntValues: intValues.ToArray(),
			FloatValues: floatValues.ToArray(),
			StringValues: stringValues.ToArray(),
			StepCount: recipe.StepCount);
	}

	public Recipe ToRecipe(PlcRecipeData data)
	{
		if (data.StepCount == 0)
		{
			return Recipe.Empty;
		}

		var intIndex = 0;
		var floatIndex = 0;
		var stringIndex = 0;

		var steps = new List<Step>(data.StepCount);

		for (var stepIndex = 0; stepIndex < data.StepCount; stepIndex++)
		{
			if (intIndex >= data.IntValues.Length)
			{
				throw new InvalidOperationException(
					$"Insufficient int values at step {stepIndex}: expected ActionKey but reached end of array");
			}

			var actionKey = data.IntValues[intIndex++];
			var action = configRegistry.GetAction(actionKey).Value;

			var properties1 = ImmutableDictionary.CreateBuilder<PropertyId, PropertyValue>();

			foreach (var columnDef in action.Properties)
			{
				var columnId = new PropertyId(columnDef.Key);
				var propertyDef = configRegistry.GetProperty(columnDef.PropertyTypeId).Value;
				var propertyType = PropertyTypeMapping.FromSystemType(propertyDef.SystemType);

				PropertyValue propertyValue;

				switch (propertyType)
				{
					case PropertyType.Int:
						if (intIndex >= data.IntValues.Length)
						{
							throw new InvalidOperationException(
								$"Insufficient int values at step {stepIndex}, column '{columnDef.Key}'");
						}
						propertyValue = PropertyValue.FromInt(data.IntValues[intIndex++]);

						break;

					case PropertyType.Float:
						if (floatIndex >= data.FloatValues.Length)
						{
							throw new InvalidOperationException(
								$"Insufficient float values at step {stepIndex}, column '{columnDef.Key}'");
						}
						propertyValue = PropertyValue.FromFloat(data.FloatValues[floatIndex++]);

						break;

					case PropertyType.String:
						if (stringIndex >= data.StringValues.Length)
						{
							throw new InvalidOperationException(
								$"Insufficient string values at step {stepIndex}, column '{columnDef.Key}'");
						}
						propertyValue = PropertyValue.FromString(data.StringValues[stringIndex++]);

						break;

					default:
						throw new InvalidOperationException($"Unknown property type: {propertyType}");
				}

				properties1.Add(columnId, propertyValue);
			}

			steps.Add(new Step(actionKey, properties1.ToImmutable()));
		}

		return new Recipe(steps.ToImmutableList());
	}

	private static void AppendDefaultValue(
		PropertyType type,
		List<int> intValues,
		List<float> floatValues,
		List<string> stringValues)
	{
		switch (type)
		{
			case PropertyType.Int:
				intValues.Add(0);

				break;
			case PropertyType.Float:
				floatValues.Add(0f);

				break;
			case PropertyType.String:
				stringValues.Add(string.Empty);

				break;
		}
	}
}

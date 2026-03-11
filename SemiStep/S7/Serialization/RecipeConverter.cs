using System.Collections.Immutable;

using Shared.Config.Contracts;
using Shared.Core;
using Shared.Plc;

namespace S7.Serialization;

internal sealed class RecipeConverter(IActionRegistry actions, IPropertyRegistry properties)
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

			var action = actions.GetAction(step.ActionKey);

			foreach (var columnDef in action.Columns)
			{
				var columnId = new ColumnId(columnDef.Key);
				var propertyDef = properties.GetProperty(columnDef.PropertyTypeId);
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
			var action = actions.GetAction(actionKey);

			var properties1 = ImmutableDictionary.CreateBuilder<ColumnId, PropertyValue>();

			foreach (var columnDef in action.Columns)
			{
				var columnId = new ColumnId(columnDef.Key);
				var propertyDef = properties.GetProperty(columnDef.PropertyTypeId);
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

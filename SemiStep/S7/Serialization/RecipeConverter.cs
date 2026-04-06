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
			SerialiseStep(step, intValues, floatValues, stringValues);
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
			steps.Add(DeserialiseStep(stepIndex, data, ref intIndex, ref floatIndex, ref stringIndex));
		}

		return new Recipe(steps.ToImmutableList());
	}

	private void SerialiseStep(
		Step step,
		List<int> intValues,
		List<float> floatValues,
		List<string> stringValues)
	{
		intValues.Add(step.ActionKey);

		var action = ResolveAction(step.ActionKey);

		foreach (var propertyDef in action.Properties)
		{
			step.Properties.TryGetValue(new PropertyId(propertyDef.Key), out var value);
			SerialiseProperty(propertyDef, value, intValues, floatValues, stringValues);
		}
	}

	private Step DeserialiseStep(
		int stepIndex,
		PlcRecipeData data,
		ref int intIndex,
		ref int floatIndex,
		ref int stringIndex)
	{
		if (intIndex >= data.IntValues.Length)
		{
			throw new InvalidOperationException(
				$"Insufficient int values at step {stepIndex}: expected ActionKey but reached end of array");
		}

		var actionKey = data.IntValues[intIndex++];
		var action = ResolveAction(actionKey);

		var properties = ImmutableDictionary.CreateBuilder<PropertyId, PropertyValue>();

		foreach (var propertyDef in action.Properties)
		{
			var value = DeserialiseProperty(propertyDef, stepIndex, data, ref intIndex, ref floatIndex, ref stringIndex);
			properties.Add(new PropertyId(propertyDef.Key), value);
		}

		return new Step(actionKey, properties.ToImmutable());
	}

	private void SerialiseProperty(
		ActionPropertyDefinition propertyDef,
		PropertyValue? value,
		List<int> intValues,
		List<float> floatValues,
		List<string> stringValues)
	{
		var propertyType = ResolvePropertyType(propertyDef);

		if (value is null)
		{
			AppendDefaultValue(propertyType, intValues, floatValues, stringValues);
			return;
		}

		switch (propertyType)
		{
			case PropertyType.Int:
				intValues.Add(value.AsInt());
				break;
			case PropertyType.Float:
				floatValues.Add(value.AsFloat());
				break;
			case PropertyType.String:
				stringValues.Add(value.AsString());
				break;
		}
	}

	private PropertyValue DeserialiseProperty(
		ActionPropertyDefinition propertyDef,
		int stepIndex,
		PlcRecipeData data,
		ref int intIndex,
		ref int floatIndex,
		ref int stringIndex)
	{
		var propertyType = ResolvePropertyType(propertyDef);

		switch (propertyType)
		{
			case PropertyType.Int:
				if (intIndex >= data.IntValues.Length)
				{
					throw new InvalidOperationException(
						$"Insufficient int values at step {stepIndex}, column '{propertyDef.Key}'");
				}
				return PropertyValue.FromInt(data.IntValues[intIndex++]);

			case PropertyType.Float:
				if (floatIndex >= data.FloatValues.Length)
				{
					throw new InvalidOperationException(
						$"Insufficient float values at step {stepIndex}, column '{propertyDef.Key}'");
				}
				return PropertyValue.FromFloat(data.FloatValues[floatIndex++]);

			case PropertyType.String:
				if (stringIndex >= data.StringValues.Length)
				{
					throw new InvalidOperationException(
						$"Insufficient string values at step {stepIndex}, column '{propertyDef.Key}'");
				}
				return PropertyValue.FromString(data.StringValues[stringIndex++]);

			default:
				throw new InvalidOperationException($"Unknown property type: {propertyType}");
		}
	}

	private PropertyType ResolvePropertyType(ActionPropertyDefinition propertyDef)
	{
		var propertyDefResult = configRegistry.GetProperty(propertyDef.PropertyTypeId);
		if (propertyDefResult.IsFailed)
		{
			throw new InvalidOperationException(
				$"Property type '{propertyDef.PropertyTypeId}' not found in configuration registry");
		}

		return PropertyTypeMapping.FromSystemType(propertyDefResult.Value.SystemType);
	}

	private ActionDefinition ResolveAction(int actionKey)
	{
		var actionResult = configRegistry.GetAction(actionKey);
		if (actionResult.IsFailed)
		{
			throw new InvalidOperationException(
				$"Action with key {actionKey} not found in configuration registry");
		}

		return actionResult.Value;
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

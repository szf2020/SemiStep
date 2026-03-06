using System.Collections.Immutable;
using System.Globalization;

using Core.Entities;

using Csv.Reasons;

using CsvHelper;
using CsvHelper.Configuration;

using Domain.Models;

using Shared.Entities;
using Shared.Reasons;
using Shared.Registries;

namespace Csv.Services;

public sealed class CsvSerializer(
	IColumnRegistry columnRegistry,
	IActionRegistry actionRegistry,
	IPropertyRegistry propertyRegistry)
{
	private const char Separator = ';';
	private const string ActionColumnKey = "action";

	public string Serialize(Recipe recipe)
	{
		var csvColumns = GetCsvColumns();
		using var stringWriter = new StringWriter();
		using var csvWriter = CreateWriter(stringWriter);

		WriteHeader(csvWriter, csvColumns);
		WriteSteps(csvWriter, recipe, csvColumns);

		csvWriter.Flush();

		return stringWriter.ToString();
	}

	public CsvLoadResult Deserialize(string csvBody)
	{
		var csvColumns = GetCsvColumns();
		var reasons = new List<AbstractReason>();

		using var stringReader = new StringReader(csvBody);
		using var csvReader = CreateReader(stringReader);

		csvReader.Read();
		csvReader.ReadHeader();

		if (!ValidateHeader(csvReader, csvColumns, reasons))
		{
			return CsvLoadResult.Failure(reasons);
		}

		var steps = new List<Step>();
		var rowNumber = 1;

		while (csvReader.Read())
		{
			rowNumber++;
			var step = TryParseStep(csvReader, csvColumns, rowNumber, reasons);
			if (step is not null)
			{
				steps.Add(step);
			}
		}

		if (reasons.OfType<AbstractError>().Any())
		{
			return CsvLoadResult.Failure(reasons);
		}

		var recipe = new Recipe(steps.ToImmutableList());
		return CsvLoadResult.Success(recipe, reasons);
	}

	private IReadOnlyList<GridColumnDefinition> GetCsvColumns()
	{
		return columnRegistry.GetAll()
			.Where(c => c.SaveToCsv)
			.ToList();
	}

	private static CsvWriter CreateWriter(TextWriter textWriter)
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = Separator.ToString(),
			HasHeaderRecord = false,
			TrimOptions = TrimOptions.Trim,
		};

		return new CsvWriter(textWriter, config);
	}

	private static CsvReader CreateReader(TextReader textReader)
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = Separator.ToString(),
			HasHeaderRecord = true,
			TrimOptions = TrimOptions.Trim,
			MissingFieldFound = null,
		};

		return new CsvReader(textReader, config);
	}

	private static void WriteHeader(CsvWriter csvWriter, IReadOnlyList<GridColumnDefinition> columns)
	{
		foreach (var column in columns)
		{
			csvWriter.WriteField(column.Key);
		}

		csvWriter.NextRecord();
	}

	private void WriteSteps(CsvWriter csvWriter, Recipe recipe, IReadOnlyList<GridColumnDefinition> columns)
	{
		foreach (var step in recipe.Steps)
		{
			WriteStep(csvWriter, step, columns);
		}
	}

	private void WriteStep(CsvWriter csvWriter, Step step, IReadOnlyList<GridColumnDefinition> columns)
	{
		foreach (var column in columns)
		{
			var value = FormatStepValue(step, column);
			csvWriter.WriteField(value);
		}

		csvWriter.NextRecord();
	}

	private string FormatStepValue(Step step, GridColumnDefinition column)
	{
		if (column.Key == ActionColumnKey)
		{
			return step.ActionKey.ToString(CultureInfo.InvariantCulture);
		}

		var columnId = new ColumnId(column.Key);
		if (!step.Properties.TryGetValue(columnId, out var propertyValue))
		{
			return string.Empty;
		}

		return propertyValue.Type switch
		{
			PropertyType.Int => propertyValue.AsInt().ToString(CultureInfo.InvariantCulture),
			PropertyType.Float => propertyValue.AsFloat().ToString("R", CultureInfo.InvariantCulture),
			PropertyType.String => propertyValue.AsString(),
			_ => propertyValue.Value?.ToString() ?? string.Empty
		};
	}

	private static bool ValidateHeader(
		CsvReader csvReader,
		IReadOnlyList<GridColumnDefinition> expectedColumns,
		List<AbstractReason> reasons)
	{
		var actualHeader = csvReader.HeaderRecord;
		if (actualHeader is null || actualHeader.Length == 0)
		{
			var expectedKeys = expectedColumns.Select(c => c.Key).ToArray();
			reasons.Add(new CsvLoadError(
				$"CSV header mismatch. Expected: [{string.Join("; ", expectedKeys)}], Actual: []"));
			return false;
		}

		var expected = expectedColumns.Select(c => c.Key).ToArray();
		var trimmedActual = actualHeader.Select(h => h.Trim()).ToArray();

		if (!expected.SequenceEqual(trimmedActual))
		{
			reasons.Add(new CsvLoadError(
				$"CSV header mismatch. Expected: [{string.Join("; ", expected)}], " +
				$"Actual: [{string.Join("; ", trimmedActual)}]"));
			return false;
		}

		return true;
	}

	private Step? TryParseStep(
		CsvReader csvReader,
		IReadOnlyList<GridColumnDefinition> columns,
		int rowNumber,
		List<AbstractReason> reasons)
	{
		var actionKey = TryParseActionKey(csvReader, rowNumber, reasons);
		if (actionKey is null)
		{
			return null;
		}

		if (!actionRegistry.ActionExists(actionKey.Value))
		{
			reasons.Add(new CsvLoadError($"Row {rowNumber}: unknown action ID '{actionKey.Value}'"));
			return null;
		}

		var action = actionRegistry.GetAction(actionKey.Value);
		var actionColumnKeys = action.Columns
			.Select(c => c.Key)
			.ToHashSet();

		var properties = ImmutableDictionary.CreateBuilder<ColumnId, PropertyValue>();

		foreach (var column in columns)
		{
			if (column.Key == ActionColumnKey)
			{
				continue;
			}

			var rawValue = csvReader.GetField(column.Key);
			if (string.IsNullOrWhiteSpace(rawValue))
			{
				continue;
			}

			if (!actionColumnKeys.Contains(column.Key))
			{
				continue;
			}

			var propertyDef = propertyRegistry.GetProperty(column.PropertyTypeId);
			var propertyValue = TryParsePropertyValue(rawValue, propertyDef, column.Key, rowNumber, reasons);
			if (propertyValue is not null)
			{
				properties.Add(new ColumnId(column.Key), propertyValue);
			}
		}

		return new Step(actionKey.Value, properties.ToImmutable());
	}

	private static int? TryParseActionKey(CsvReader csvReader, int rowNumber, List<AbstractReason> reasons)
	{
		var rawAction = csvReader.GetField(ActionColumnKey);
		if (string.IsNullOrWhiteSpace(rawAction))
		{
			reasons.Add(new CsvLoadError($"Row {rowNumber}: action column is empty"));
			return null;
		}

		if (!int.TryParse(rawAction, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionKey))
		{
			reasons.Add(new CsvLoadError($"Row {rowNumber}: cannot parse action value '{rawAction}' as integer"));
			return null;
		}

		return actionKey;
	}

	private static PropertyValue? TryParsePropertyValue(
		string rawValue,
		PropertyDefinition propertyDef,
		string columnKey,
		int rowNumber,
		List<AbstractReason> reasons)
	{
		return propertyDef.SystemType.ToLowerInvariant() switch
		{
			"int" => TryParseIntProperty(rawValue, columnKey, rowNumber, reasons),
			"float" => TryParseFloatProperty(rawValue, columnKey, rowNumber, reasons),
			"string" => PropertyValue.FromString(rawValue),
			_ => HandleUnknownSystemType(propertyDef.SystemType, columnKey, rowNumber, reasons)
		};
	}

	private static PropertyValue? TryParseIntProperty(
		string rawValue, string columnKey, int rowNumber, List<AbstractReason> reasons)
	{
		if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
		{
			reasons.Add(new CsvLoadError(
				$"Row {rowNumber}: cannot parse value '{rawValue}' as int for column '{columnKey}'"));
			return null;
		}

		return PropertyValue.FromInt(intValue);
	}

	private static PropertyValue? TryParseFloatProperty(
		string rawValue, string columnKey, int rowNumber, List<AbstractReason> reasons)
	{
		if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
		{
			reasons.Add(new CsvLoadError(
				$"Row {rowNumber}: cannot parse value '{rawValue}' as float for column '{columnKey}'"));
			return null;
		}

		return PropertyValue.FromFloat(floatValue);
	}

	private static PropertyValue? HandleUnknownSystemType(
		string systemType, string columnKey, int rowNumber, List<AbstractReason> reasons)
	{
		reasons.Add(new CsvLoadError(
			$"Row {rowNumber}: unknown system type '{systemType}' for column '{columnKey}'"));
		return null;
	}
}

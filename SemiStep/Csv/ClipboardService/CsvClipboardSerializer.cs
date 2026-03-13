using System.Collections.Immutable;
using System.Globalization;

using Csv.FsService;

using CsvHelper;
using CsvHelper.Configuration;

using FluentResults;

using Shared.Config;
using Shared.Config.Contracts;
using Shared.Core;

namespace Csv.ClipboardService;

internal sealed class CsvClipboardSerializer(
	IColumnRegistry columnRegistry,
	IActionRegistry actionRegistry,
	IPropertyRegistry propertyRegistry)
{
	private const char ClipboardSeparator = '\t';
	private const string ActionColumnKey = "action";

	internal string SerializeSteps(IReadOnlyList<Step> steps)
	{
		var csvColumns = GetCsvColumns();
		using var stringWriter = new StringWriter();
		using var csvWriter = CreateClipboardWriter(stringWriter);

		foreach (var step in steps)
		{
			WriteStep(csvWriter, step, csvColumns);
		}

		csvWriter.Flush();

		return stringWriter.ToString();
	}

	internal Result<IReadOnlyList<Step>> DeserializeSteps(string csvBody)
	{
		var csvColumns = GetCsvColumns();

		using var stringReader = new StringReader(csvBody);
		using var csvReader = CreateClipboardReader(stringReader);

		try
		{
			var steps = new List<Step>();
			var rowNumber = 0;
			var errors = new List<IError>();

			while (csvReader.Read())
			{
				rowNumber++;
				var stepResult = TryParseStepByIndex(csvReader, csvColumns, rowNumber);
				if (stepResult.IsFailed)
				{
					errors.AddRange(stepResult.Errors);
					continue;
				}
				steps.Add(stepResult.Value);
			}

			if (errors.Count > 0)
			{
				return Result.Fail<IReadOnlyList<Step>>(errors);
			}

			if (steps.Count == 0)
			{
				return Result.Fail<IReadOnlyList<Step>>("No valid steps found in clipboard data");
			}

			return Result.Ok<IReadOnlyList<Step>>(steps);
		}
		catch (Exception ex)
		{
			return Result.Fail<IReadOnlyList<Step>>($"Failed to parse clipboard data: {ex.Message}");
		}
	}

	private IReadOnlyList<GridColumnDefinition> GetCsvColumns()
	{
		return columnRegistry.GetAll()
			.Where(c => c.SaveToCsv)
			.ToList();
	}

	private static CsvWriter CreateClipboardWriter(TextWriter textWriter)
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = ClipboardSeparator.ToString(),
			HasHeaderRecord = false,
			TrimOptions = TrimOptions.Trim,
		};

		return new CsvWriter(textWriter, config);
	}

	private static CsvReader CreateClipboardReader(TextReader textReader)
	{
		var config = new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			Delimiter = ClipboardSeparator.ToString(),
			HasHeaderRecord = false,
			TrimOptions = TrimOptions.Trim,
			MissingFieldFound = null,
		};

		return new CsvReader(textReader, config);
	}

	private static void WriteStep(CsvWriter csvWriter, Step step, IReadOnlyList<GridColumnDefinition> columns)
	{
		foreach (var column in columns)
		{
			var value = StepValueParser.FormatStepValue(step, column);
			csvWriter.WriteField(value);
		}

		csvWriter.NextRecord();
	}

	private Result<Step> TryParseStepByIndex(
		CsvReader csvReader,
		IReadOnlyList<GridColumnDefinition> columns,
		int rowNumber)
	{
		var actionColumnIndex = -1;
		for (var i = 0; i < columns.Count; i++)
		{
			if (columns[i].Key == ActionColumnKey)
			{
				actionColumnIndex = i;

				break;
			}
		}

		if (actionColumnIndex < 0)
		{
			return Result.Fail($"Row {rowNumber}: action column not found in configuration");
		}

		var rawAction = csvReader.GetField(actionColumnIndex);
		if (string.IsNullOrWhiteSpace(rawAction) ||
			!int.TryParse(rawAction, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actionKey))
		{
			return Result.Fail($"Row {rowNumber}: cannot parse action value '{rawAction}' as integer");
		}

		if (!actionRegistry.ActionExists(actionKey))
		{
			return Result.Fail($"Row {rowNumber}: unknown action ID '{actionKey}'");
		}

		var action = actionRegistry.GetAction(actionKey);
		var actionColumnKeys = action.Columns
			.Select(c => c.Key)
			.ToHashSet();

		var errors = new List<IError>();
		var properties = ImmutableDictionary.CreateBuilder<ColumnId, PropertyValue>();

		for (var i = 0; i < columns.Count; i++)
		{
			var column = columns[i];
			if (column.Key == ActionColumnKey)
			{
				continue;
			}

			var rawValue = csvReader.GetField(i);
			if (string.IsNullOrWhiteSpace(rawValue))
			{
				continue;
			}

			if (!actionColumnKeys.Contains(column.Key))
			{
				continue;
			}

			var propertyDef = propertyRegistry.GetProperty(column.PropertyTypeId);
			var propertyResult = StepValueParser.TryParsePropertyValue(rawValue, propertyDef, column.Key, rowNumber);
			if (propertyResult.IsFailed)
			{
				errors.AddRange(propertyResult.Errors);
			}
			else
			{
				properties.Add(new ColumnId(column.Key), propertyResult.Value);
			}
		}

		if (errors.Count > 0)
		{
			return Result.Fail(errors);
		}

		return new Step(actionKey, properties.ToImmutable());
	}
}

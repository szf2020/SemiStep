using System.Collections.Immutable;
using System.Text;

using Csv.Services;

using Serilog;

using Shared.Core;
using Shared.Csv;
using Shared.ServiceContracts;

namespace Csv.Facade;

internal sealed class CsvService(CsvSerializer csvSerializer) : ICsvService
{
	private static readonly Encoding _fileEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

	public async Task<CsvLoadResult> LoadAsync(string filePath, CancellationToken cancellationToken = default)
	{
		if (!File.Exists(filePath))
		{
			return CsvLoadResult.Failure([$"Recipe file not found: {filePath}"]);
		}

		var fullText = await File.ReadAllTextAsync(filePath, _fileEncoding, cancellationToken);

		var (metadata, linesConsumed) = CsvMetadata.Deserialize(fullText);
		var bodyText = ExtractBody(fullText, linesConsumed);

		var result = csvSerializer.Deserialize(bodyText);

		if (!result.IsSuccess)
		{
			return result;
		}

		var warnings = new List<string>(result.Warnings);

		if (metadata.Rows > 0 && metadata.Rows != result.Recipe!.StepCount)
		{
			warnings.Add(
				$"Row count mismatch in '{filePath}': metadata says {metadata.Rows}, actual is {result.Recipe.StepCount}");
		}

		Log.Information("Loaded recipe from {FilePath}: {StepCount} steps", filePath, result.Recipe!.StepCount);

		return CsvLoadResult.Success(result.Recipe, warnings);
	}

	public async Task SaveAsync(Recipe recipe, string filePath, CancellationToken cancellationToken = default)
	{
		var csvBody = csvSerializer.Serialize(recipe);
		var dataRowCount = CountDataRows(csvBody);

		var metadata = new CsvMetadata(
			Rows: dataRowCount,
			Extras: ImmutableDictionary<string, string>.Empty
				.Add("ExportedAtUtc", DateTime.UtcNow.ToString("O")));

		var tempPath = filePath + ".tmp";

		try
		{
			await using (var stream = new FileStream(
							 tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
			await using (var writer = new StreamWriter(stream, _fileEncoding))
			{
				CsvMetadata.Serialize(writer, metadata);
				await writer.WriteAsync(csvBody);
			}

			File.Move(tempPath, filePath, overwrite: true);
		}
		finally
		{
			if (File.Exists(tempPath))
			{
				try
				{
					File.Delete(tempPath);
				}
				catch
				{
					// Best effort cleanup
				}
			}
		}

		Log.Information("Saved recipe to {FilePath}: {StepCount} steps", filePath, recipe.StepCount);
	}

	private static string ExtractBody(string fullText, int metadataLines)
	{
		if (metadataLines == 0)
		{
			return fullText;
		}

		using var reader = new StringReader(fullText);
		for (var i = 0; i < metadataLines; i++)
		{
			reader.ReadLine();
		}

		return reader.ReadToEnd();
	}

	private static int CountDataRows(string csvBody)
	{
		if (string.IsNullOrEmpty(csvBody))
		{
			return 0;
		}

		using var reader = new StringReader(csvBody);
		var lineCount = 0;

		while (reader.ReadLine() is not null)
		{
			lineCount++;
		}

		return Math.Max(0, lineCount - 1);
	}
}

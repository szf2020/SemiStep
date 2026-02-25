using Config.Dto;
using Config.Models;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Config.Loaders;

public sealed class ConnectionLoader
{
	private static readonly IDeserializer _deserializer = new DeserializerBuilder()
		.WithNamingConvention(UnderscoredNamingConvention.Instance)
		.Build();

	public static async Task<ConnectionDto?> LoadAsync(string configDirectory, ConfigContext context)
	{
		var connectionDir = Path.Combine(configDirectory, "connection");

		if (!Directory.Exists(connectionDir))
		{
			context.AddInfo($"Connection directory not found, using default PLC settings: {connectionDir}");

			return null;
		}

		var filePath = Path.Combine(connectionDir, "connection.yaml");

		if (!File.Exists(filePath))
		{
			context.AddInfo($"Connection file not found, using defaults: {filePath}");

			return null;
		}

		try
		{
			var content = await File.ReadAllTextAsync(filePath);
			var dto = _deserializer.Deserialize<ConnectionDto>(content);

			if (dto == null)
			{
				context.AddWarning("Connection file is empty, using defaults", filePath);

				return null;
			}

			return dto;
		}
		catch (Exception ex)
		{
			context.AddWarning($"Failed to parse connection file, using defaults: {ex.Message}", filePath);

			return null;
		}
	}
}

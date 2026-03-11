using Config.Dto;

using Shared;
using Shared.Config;

namespace Config.Models;

internal sealed class ConfigContext
{
	private readonly List<string> _errors = [];
	private readonly List<string> _warnings = [];

	public List<string> FilePaths { get; init; } = [];

	public List<ActionDto>? Actions { get; set; }

	public List<ColumnDto>? Columns { get; set; }

	public List<PropertyDto>? Properties { get; set; }

	public Dictionary<string, Dictionary<int, string>>? Groups { get; set; }

	public GridStyleOptionsDto? GridStyle { get; set; }

	public ConnectionDto? Connection { get; set; }

	public AppConfiguration? Configuration { get; set; }

	public Dictionary<string, object> Metadata { get; } = [];

	public bool HasErrors => _errors.Count > 0;

	public bool HasWarnings => _warnings.Count > 0;

	public IReadOnlyList<string> Errors => _errors;

	public IReadOnlyList<string> Warnings => _warnings;

	public void AddError(string message, string? location = null)
	{
		_errors.Add(location is not null ? $"[{location}] {message}" : message);
	}

	public void AddWarning(string message, string? location = null)
	{
		_warnings.Add(location is not null ? $"[{location}] {message}" : message);
	}

	public void AddInfo(string message, string? location = null)
	{
		_warnings.Add(location is not null ? $"[{location}] {message}" : message);
	}
}

using Config.Dto;

using Shared;
using Shared.Reasons;

namespace Config.Models;

public sealed class ConfigContext
{
	public List<string> FilePaths { get; init; } = [];

	public List<ActionDto>? Actions { get; set; }

	public List<ColumnDto>? Columns { get; set; }

	public List<PropertyDto>? Properties { get; set; }

	public Dictionary<string, Dictionary<int, string>>? Groups { get; set; }

	public GridStyleOptionsDto? GridStyle { get; set; }

	public ConnectionDto? Connection { get; set; }

	public AppConfiguration? Configuration { get; set; }

	public List<AbstractReason> Reasons { get; } = [];

	public Dictionary<string, object> Metadata { get; } = [];

	public bool HasErrors => Reasons.Any(r => r is AbstractError);

	public bool HasWarnings => Reasons.Any(r => r is AbstractWarning);

	public IEnumerable<AbstractError> Errors => Reasons.OfType<AbstractError>();

	public IEnumerable<AbstractWarning> Warnings => Reasons.OfType<AbstractWarning>();

	public void AddError(string message, string? location = null)
	{
		Reasons.Add(ConfigLoadError.General(message, location));
	}

	public void AddWarning(string message, string? location = null)
	{
		Reasons.Add(ConfigWarning.General(message, location));
	}

	public void AddInfo(string message, string? location = null)
	{
		// Info messages are treated as warnings (informational, non-blocking)
		Reasons.Add(ConfigWarning.General(message, location));
	}

	public void Add(AbstractReason reason)
	{
		Reasons.Add(reason);
	}
}

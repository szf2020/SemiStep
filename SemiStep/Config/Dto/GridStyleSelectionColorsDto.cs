using YamlDotNet.Serialization;

namespace Config.Dto;

public sealed class GridStyleSelectionColorsDto
{
	[YamlMember(Alias = "background")] public string? Background { get; set; }

	[YamlMember(Alias = "foreground")] public string? Foreground { get; set; }
}

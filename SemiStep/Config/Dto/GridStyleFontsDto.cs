using YamlDotNet.Serialization;

namespace Config.Dto;

public sealed class GridStyleFontsDto
{
	[YamlMember(Alias = "header_size")] public int? HeaderSize { get; set; }

	[YamlMember(Alias = "cell_size")] public int? CellSize { get; set; }
}

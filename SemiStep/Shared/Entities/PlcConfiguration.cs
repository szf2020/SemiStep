namespace Shared.Entities;

public sealed record PlcConfiguration(
	PlcConnectionSettings Connection,
	PlcProtocolSettings ProtocolSettings,
	PlcProtocolLayout Layout)
{
	public static PlcConfiguration Default => new(
		Connection: PlcConnectionSettings.Default,
		ProtocolSettings: PlcProtocolSettings.Default,
		Layout: PlcProtocolLayout.Default);
}

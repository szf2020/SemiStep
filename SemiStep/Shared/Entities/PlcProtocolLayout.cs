namespace Shared.Entities;

public sealed record PlcProtocolLayout(
	HeaderDbLayout HeaderDb,
	ManagingDbLayout ManagingDb,
	DataDbLayout IntDb,
	DataDbLayout FloatDb,
	DataDbLayout StringDb,
	ExecutionDbLayout ExecutionDb)
{
	public static PlcProtocolLayout Default => new(
		HeaderDb: HeaderDbLayout.Default,
		ManagingDb: ManagingDbLayout.Default,
		IntDb: DataDbLayout.DefaultInt,
		FloatDb: DataDbLayout.DefaultFloat,
		StringDb: DataDbLayout.DefaultString,
		ExecutionDb: ExecutionDbLayout.Default);
}

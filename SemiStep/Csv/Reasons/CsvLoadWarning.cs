using Shared.Reasons;

namespace Csv.Reasons;

public sealed record CsvLoadWarning(string Message) : AbstractWarning(Message);

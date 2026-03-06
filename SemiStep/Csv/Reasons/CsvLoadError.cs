using Shared.Reasons;

namespace Csv.Reasons;

public sealed record CsvLoadError(string Message) : AbstractError(Message);

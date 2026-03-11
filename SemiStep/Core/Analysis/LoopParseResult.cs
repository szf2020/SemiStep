using Shared.Core;

namespace Core.Analysis;

internal sealed record LoopParseResult(IReadOnlyList<LoopInfo> Loops, IReadOnlyList<string> Errors);

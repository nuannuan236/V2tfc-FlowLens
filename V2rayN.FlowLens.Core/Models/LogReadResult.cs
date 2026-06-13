namespace V2rayN.FlowLens.Core.Models;

public sealed record LogReadResult(
    IReadOnlyList<LogConnectionRecord> Records,
    IReadOnlyList<string> Files);

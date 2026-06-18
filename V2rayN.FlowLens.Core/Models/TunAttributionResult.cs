namespace V2rayN.FlowLens.Core.Models;

public sealed record TunAttributionResult(
    IReadOnlyList<AttributedConnection> Connections,
    TunAttributionDiagnostics Diagnostics);

namespace V2rayN.FlowLens.Core.Models;

public sealed record LogConnectionRecord(
    DateTime Timestamp,
    string SourceAddress,
    int SourcePort,
    string TargetHost,
    int? TargetPort,
    string Inbound,
    string Outbound,
    string RawLine);

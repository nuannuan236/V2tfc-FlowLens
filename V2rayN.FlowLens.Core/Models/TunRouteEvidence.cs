namespace V2rayN.FlowLens.Core.Models;

public sealed record TunRouteEvidence(
    DateTime Timestamp,
    string TargetHost,
    int? TargetPort,
    string Inbound,
    string Outbound,
    string RawLine);

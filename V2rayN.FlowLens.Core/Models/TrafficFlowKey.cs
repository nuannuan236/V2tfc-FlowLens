namespace V2rayN.FlowLens.Core.Models;

public sealed record TrafficFlowKey(
    int ProcessId,
    string LocalAddress,
    int LocalPort,
    string RemoteAddress,
    int RemotePort);

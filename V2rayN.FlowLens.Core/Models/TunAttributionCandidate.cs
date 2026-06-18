namespace V2rayN.FlowLens.Core.Models;

public sealed record TunAttributionCandidate(
    string Application,
    int ProcessId,
    string LocalAddress,
    int LocalPort,
    string RemoteAddress,
    int RemotePort,
    DateTime LastSeen,
    long SentBytes,
    long ReceivedBytes);

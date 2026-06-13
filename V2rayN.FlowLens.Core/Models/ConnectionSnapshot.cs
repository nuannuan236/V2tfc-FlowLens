namespace V2rayN.FlowLens.Core.Models;

public sealed record ConnectionSnapshot(
    TcpConnectionInfo Connection,
    DateTime LastSeen,
    bool IsCurrent);

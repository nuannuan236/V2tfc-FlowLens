namespace V2rayN.FlowLens.Core.Models;

public sealed record TcpConnectionInfo(
    string LocalAddress,
    int LocalPort,
    string RemoteAddress,
    int RemotePort,
    int ProcessId,
    string ProcessName,
    string State);

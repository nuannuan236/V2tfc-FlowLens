namespace V2rayN.FlowLens.Core.Models;

public sealed record AttributedConnection(
    DateTime? Timestamp,
    string Application,
    int? ProcessId,
    int SourcePort,
    string Target,
    string Inbound,
    string Outbound,
    string Status,
    long SentBytes,
    long ReceivedBytes,
    DateTime LastSeen)
{
    public long TotalBytes => SentBytes + ReceivedBytes;

    public string SentTraffic => V2rayN.FlowLens.Core.ByteFormatter.Format(SentBytes);

    public string ReceivedTraffic => V2rayN.FlowLens.Core.ByteFormatter.Format(ReceivedBytes);

    public string TotalTraffic => V2rayN.FlowLens.Core.ByteFormatter.Format(TotalBytes);
}

namespace V2rayN.FlowLens.Core.Models;

public sealed record TrafficCounters(long SentBytes, long ReceivedBytes)
{
    public long TotalBytes => SentBytes + ReceivedBytes;
}

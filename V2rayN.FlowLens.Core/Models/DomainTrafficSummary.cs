namespace V2rayN.FlowLens.Core.Models;

public sealed record DomainTrafficSummary(
    string Domain,
    int ConnectionCount,
    string Applications,
    int ProxyCount,
    int DirectCount,
    int UnknownCount,
    long TotalBytes,
    long ProxyBytes,
    long DirectBytes,
    long UnknownBytes,
    DateTime LastSeen)
{
    public string TotalTraffic => V2rayN.FlowLens.Core.ByteFormatter.Format(TotalBytes);

    public string ProxyTraffic => V2rayN.FlowLens.Core.ByteFormatter.Format(ProxyBytes);

    public string DirectTraffic => V2rayN.FlowLens.Core.ByteFormatter.Format(DirectBytes);

    public string UnknownTraffic => V2rayN.FlowLens.Core.ByteFormatter.Format(UnknownBytes);
}

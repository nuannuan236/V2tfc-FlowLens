using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public static class TrafficSummaryFilter
{
    public static IReadOnlyList<ApplicationTrafficSummary> FilterApplications(
        IEnumerable<ApplicationTrafficSummary> rows,
        string keyword,
        string outbound)
    {
        return rows
            .Where(row => MatchesKeyword(row.Application, keyword))
            .Where(row => MatchesApplicationOutbound(row, outbound))
            .ToArray();
    }

    public static IReadOnlyList<DomainTrafficSummary> FilterDomains(
        IEnumerable<DomainTrafficSummary> rows,
        string keyword,
        string outbound)
    {
        return rows
            .Where(row => MatchesKeyword(row.Domain, keyword) || MatchesKeyword(row.Applications, keyword))
            .Where(row => MatchesDomainOutbound(row, outbound))
            .ToArray();
    }

    public static IReadOnlyList<AttributedConnection> FilterConnections(
        IEnumerable<AttributedConnection> rows,
        string keyword,
        string outbound)
    {
        return rows
            .Where(row =>
                MatchesKeyword(row.Application, keyword) ||
                MatchesKeyword(row.Target, keyword) ||
                MatchesKeyword(row.Inbound, keyword))
            .Where(row => MatchesOutbound(row.Outbound, outbound))
            .ToArray();
    }

    private static bool MatchesApplicationOutbound(ApplicationTrafficSummary row, string outbound)
    {
        return NormalizeOutbound(outbound) switch
        {
            "proxy" => row.ProxyCount > 0 || row.ProxyBytes > 0,
            "direct" => row.DirectCount > 0 || row.DirectBytes > 0,
            "unknown" => row.UnknownCount > 0 || row.UnknownBytes > 0,
            _ => true
        };
    }

    private static bool MatchesDomainOutbound(DomainTrafficSummary row, string outbound)
    {
        return NormalizeOutbound(outbound) switch
        {
            "proxy" => row.ProxyCount > 0 || row.ProxyBytes > 0,
            "direct" => row.DirectCount > 0 || row.DirectBytes > 0,
            "unknown" => row.UnknownCount > 0 || row.UnknownBytes > 0,
            _ => true
        };
    }

    private static bool MatchesOutbound(string value, string outbound)
    {
        var normalized = NormalizeOutbound(outbound);
        return normalized == "all" || value.Equals(normalized, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesKeyword(string value, string keyword)
    {
        return string.IsNullOrWhiteSpace(keyword) ||
            value.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeOutbound(string outbound)
    {
        return string.IsNullOrWhiteSpace(outbound) ? "all" : outbound.Trim().ToLowerInvariant();
    }
}

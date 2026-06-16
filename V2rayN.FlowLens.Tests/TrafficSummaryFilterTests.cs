using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class TrafficSummaryFilterTests
{
    [Fact]
    public void FilterApplications_MatchesKeywordAndOutbound()
    {
        var rows = new[]
        {
            new ApplicationTrafficSummary("chrome.exe", 1, 1, 1, 0, 0, 0, 100, 100, 0, 0, DateTime.Now),
            new ApplicationTrafficSummary("steam.exe", 2, 1, 0, 1, 0, 0, 200, 0, 200, 0, DateTime.Now)
        };

        var filtered = TrafficSummaryFilter.FilterApplications(rows, "chrome", "proxy");

        var row = Assert.Single(filtered);
        Assert.Equal("chrome.exe", row.Application);
    }

    [Fact]
    public void FilterDomains_MatchesDomainOrApplication()
    {
        var rows = new[]
        {
            new DomainTrafficSummary("github.com", 1, "msedge.exe", 1, 0, 0, 100, 100, 0, 0, DateTime.Now),
            new DomainTrafficSummary("baidu.com", 1, "twinkstar.exe", 0, 1, 0, 200, 0, 200, 0, DateTime.Now)
        };

        var filtered = TrafficSummaryFilter.FilterDomains(rows, "twink", "direct");

        var row = Assert.Single(filtered);
        Assert.Equal("baidu.com", row.Domain);
    }

    [Fact]
    public void FilterConnections_DoesNotMutateSource()
    {
        var rows = new[]
        {
            CreateConnection("msedge.exe", "github.com:443", "proxy"),
            CreateConnection("codex.exe", "example.com:443", "unknown")
        };

        var filtered = TrafficSummaryFilter.FilterConnections(rows, "github", "proxy");

        Assert.Single(filtered);
        Assert.Equal(2, rows.Length);
    }

    private static AttributedConnection CreateConnection(string application, string target, string outbound)
    {
        return new AttributedConnection(
            DateTime.Now,
            application,
            100,
            12000,
            target,
            "socks",
            outbound,
            "Matched",
            1,
            2,
            DateTime.Now);
    }
}

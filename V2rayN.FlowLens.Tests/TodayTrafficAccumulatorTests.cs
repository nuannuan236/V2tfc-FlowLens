using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class TodayTrafficAccumulatorTests
{
    [Fact]
    public void AddSnapshot_DoesNotDoubleCountSameConnection()
    {
        var accumulator = new TodayTrafficAccumulator();
        accumulator.Load(TodayTrafficHistory.Empty(new DateOnly(2026, 6, 16)));
        var connection = CreateConnection(totalBytes: 1000);

        accumulator.AddSnapshot([connection]);
        accumulator.AddSnapshot([connection]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(1000, summary.TotalBytes);
        Assert.Equal(1, summary.ConnectionCount);
    }

    [Fact]
    public void AddSnapshot_AccumulatesPositiveDeltaForExistingConnection()
    {
        var accumulator = new TodayTrafficAccumulator();
        accumulator.Load(TodayTrafficHistory.Empty(new DateOnly(2026, 6, 16)));

        accumulator.AddSnapshot([CreateConnection(totalBytes: 1000)]);
        accumulator.AddSnapshot([CreateConnection(totalBytes: 1500)]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(1500, summary.TotalBytes);
        Assert.Equal(1500, summary.ProxyBytes);
    }

    [Fact]
    public void AddSnapshot_SplitsBytesByOutbound()
    {
        var accumulator = new TodayTrafficAccumulator();
        accumulator.Load(TodayTrafficHistory.Empty(new DateOnly(2026, 6, 16)));

        accumulator.AddSnapshot(
        [
            CreateConnection(sourcePort: 10001, outbound: "proxy", totalBytes: 1000),
            CreateConnection(sourcePort: 10002, target: "baidu.com:443", outbound: "direct", totalBytes: 2000),
            CreateConnection(sourcePort: 10003, target: "unknown", outbound: "unknown", totalBytes: 3000)
        ]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(6000, summary.TotalBytes);
        Assert.Equal(1000, summary.ProxyBytes);
        Assert.Equal(2000, summary.DirectBytes);
        Assert.Equal(3000, summary.UnknownBytes);
    }

    [Fact]
    public void AddSnapshot_ExcludesLogOnlyConnections()
    {
        var accumulator = new TodayTrafficAccumulator();
        accumulator.Load(TodayTrafficHistory.Empty(new DateOnly(2026, 6, 16)));

        accumulator.AddSnapshot(
        [
            CreateConnection(totalBytes: 1000),
            CreateConnection(application: "Unknown", processId: null, status: "LogOnly", totalBytes: 9000)
        ]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(1000, summary.TotalBytes);
    }

    [Fact]
    public void Load_ContinuesFromPersistedSummaries()
    {
        var accumulator = new TodayTrafficAccumulator();
        accumulator.Load(new TodayTrafficHistory(
            new DateOnly(2026, 6, 16),
            [
                new ApplicationTrafficSummary("chrome.exe", 1234, 1, 1, 0, 0, 0, 500, 500, 0, 0, new DateTime(2026, 6, 16, 8, 0, 0))
            ],
            [
                new DomainTrafficSummary("google.com", 1, "chrome.exe", 1, 0, 0, 500, 500, 0, 0, new DateTime(2026, 6, 16, 8, 0, 0))
            ]));

        accumulator.AddSnapshot([CreateConnection(sourcePort: 10002, totalBytes: 700)]);

        var app = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(1200, app.TotalBytes);
        Assert.Equal(2, app.ConnectionCount);
        var domain = Assert.Single(accumulator.GetDomainSummaries());
        Assert.Equal(1200, domain.TotalBytes);
        Assert.Equal(2, domain.ConnectionCount);
    }

    private static AttributedConnection CreateConnection(
        string application = "chrome.exe",
        int? processId = 1234,
        int sourcePort = 10001,
        string target = "google.com:443",
        string inbound = "socks",
        string outbound = "proxy",
        string status = "Matched",
        long totalBytes = 1000)
    {
        return new AttributedConnection(
            new DateTime(2026, 6, 16, 8, 0, 0),
            application,
            processId,
            sourcePort,
            target,
            inbound,
            outbound,
            status,
            totalBytes,
            0,
            new DateTime(2026, 6, 16, 8, 0, 1));
    }
}

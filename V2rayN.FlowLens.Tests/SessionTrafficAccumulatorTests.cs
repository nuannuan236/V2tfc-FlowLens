using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class SessionTrafficAccumulatorTests
{
    [Fact]
    public void AddSnapshot_DoesNotDoubleCountSameConnection()
    {
        var accumulator = new SessionTrafficAccumulator();
        var connection = CreateConnection(totalBytes: 1000);

        accumulator.AddSnapshot([connection]);
        accumulator.AddSnapshot([connection]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(1000, summary.TotalBytes);
        Assert.Equal(1, summary.ConnectionCount);
    }

    [Fact]
    public void AddSnapshot_AccumulatesOnlyPositiveDeltaForExistingConnection()
    {
        var accumulator = new SessionTrafficAccumulator();

        accumulator.AddSnapshot([CreateConnection(totalBytes: 1000)]);
        accumulator.AddSnapshot([CreateConnection(totalBytes: 1500)]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(1500, summary.TotalBytes);
        Assert.Equal(1500, summary.ProxyBytes);
    }

    [Fact]
    public void AddSnapshot_SumsMultipleConnectionsForSameApplication()
    {
        var accumulator = new SessionTrafficAccumulator();

        accumulator.AddSnapshot(
        [
            CreateConnection(sourcePort: 10001, totalBytes: 1000),
            CreateConnection(sourcePort: 10002, target: "github.com:443", totalBytes: 2000)
        ]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(3000, summary.TotalBytes);
        Assert.Equal(2, summary.ConnectionCount);
    }

    [Fact]
    public void AddSnapshot_SplitsBytesByOutbound()
    {
        var accumulator = new SessionTrafficAccumulator();

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
    public void AddSnapshot_ExcludesLogOnlyConnectionsFromApplicationSummary()
    {
        var accumulator = new SessionTrafficAccumulator();

        accumulator.AddSnapshot(
        [
            CreateConnection(totalBytes: 1000),
            CreateConnection(application: "Unknown", processId: null, status: "LogOnly", totalBytes: 9000)
        ]);

        var summary = Assert.Single(accumulator.GetApplicationSummaries());
        Assert.Equal(1000, summary.TotalBytes);
    }

    [Fact]
    public void GetDomainSummaries_AccumulatesByDomain()
    {
        var accumulator = new SessionTrafficAccumulator();

        accumulator.AddSnapshot(
        [
            CreateConnection(sourcePort: 10001, target: "github.com:443", totalBytes: 1000),
            CreateConnection(sourcePort: 10002, target: "github.com:443", totalBytes: 2000)
        ]);

        var summary = Assert.Single(accumulator.GetDomainSummaries());
        Assert.Equal("github.com", summary.Domain);
        Assert.Equal(3000, summary.TotalBytes);
        Assert.Equal(2, summary.ConnectionCount);
    }

    [Fact]
    public void Reset_ClearsSummariesAndResetsStartedAt()
    {
        var accumulator = new SessionTrafficAccumulator();
        var originalStartedAt = accumulator.StartedAt;

        accumulator.AddSnapshot([CreateConnection(totalBytes: 1000)]);
        Thread.Sleep(5);
        accumulator.Reset();

        Assert.Empty(accumulator.GetApplicationSummaries());
        Assert.True(accumulator.StartedAt >= originalStartedAt);
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

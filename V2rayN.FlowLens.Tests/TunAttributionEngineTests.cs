using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class TunAttributionEngineTests
{
    [Fact]
    public void Attribute_ReturnsMatchedForUniqueIpAndPortCandidate()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("chrome.exe", row.Application);
        Assert.Equal("proxy", row.Outbound);
        Assert.Equal("Matched", row.Status);
        Assert.Equal(AttributionMode.Tun, row.AttributionMode);
        Assert.Equal(AttributionConfidence.Matched, row.Confidence);
    }

    [Fact]
    public void Attribute_ReturnsProbableForUniqueDomainPortCandidate()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "msedge.exe", 101, "20.205.243.166", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "github.com", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("msedge.exe", row.Application);
        Assert.Equal("Probable", row.Status);
        Assert.Equal(AttributionConfidence.Probable, row.Confidence);
    }

    [Fact]
    public void Attribute_ReturnsAmbiguousForMultipleCandidates()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "20.205.243.166", 443),
            CreateSnapshot(now, "msedge.exe", 101, "20.205.243.166", 443, localPort: 50001)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "20.205.243.166", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("Unknown", row.Application);
        Assert.Null(row.ProcessId);
        Assert.Equal("Ambiguous", row.Status);
        Assert.Equal(AttributionConfidence.Ambiguous, row.Confidence);
        Assert.Contains("chrome.exe", row.Evidence);
        Assert.Contains("msedge.exe", row.Evidence);
    }

    [Fact]
    public void Attribute_ReturnsUnknownWhenRouteEvidenceHasNoCandidate()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute([], logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("Unknown", row.Status);
        Assert.Equal(AttributionConfidence.Unknown, row.Confidence);
    }

    [Fact]
    public void Attribute_ReturnsUnknownTcpCandidateWhenLogsAreMissing()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };

        var result = new TunAttributionEngine().Attribute(snapshots, [], Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("chrome.exe", row.Application);
        Assert.Equal("unknown", row.Outbound);
        Assert.Equal("Unknown", row.Status);
        Assert.Equal(AttributionConfidence.Unknown, row.Confidence);
    }

    private static FlowLensSettings Settings()
    {
        return new FlowLensSettings
        {
            AttributionMode = AttributionMode.Tun,
            HideCoreProcesses = true
        };
    }

    private static Dictionary<TrafficFlowKey, TrafficCounters> Traffic()
    {
        return new Dictionary<TrafficFlowKey, TrafficCounters>();
    }

    private static ConnectionSnapshot CreateSnapshot(
        DateTime now,
        string processName,
        int processId,
        string remoteAddress,
        int remotePort,
        int localPort = 50000)
    {
        return new ConnectionSnapshot(
            new TcpConnectionInfo("192.168.1.10", localPort, remoteAddress, remotePort, processId, processName, "Established"),
            now,
            true);
    }
}

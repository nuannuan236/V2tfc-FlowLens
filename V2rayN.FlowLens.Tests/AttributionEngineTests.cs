using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class AttributionEngineTests
{
    [Fact]
    public void Attribute_MatchesApplicationConnectionBySourcePort()
    {
        var tcpConnections = new[]
        {
            new TcpConnectionInfo("127.0.0.1", 9852, "127.0.0.1", 10808, 1234, "chrome.exe", "Established")
        };
        var logRecords = new[]
        {
            new LogConnectionRecord(
                new DateTime(2026, 6, 13, 16, 39, 10),
                "127.0.0.1",
                9852,
                "www.google-analytics.com",
                443,
                "socks",
                "proxy",
                "raw")
        };
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808 }
        };

        var result = new AttributionEngine().Attribute(tcpConnections, logRecords, settings);

        var connection = Assert.Single(result);
        Assert.Equal("chrome.exe", connection.Application);
        Assert.Equal(1234, connection.ProcessId);
        Assert.Equal(9852, connection.SourcePort);
        Assert.Equal("www.google-analytics.com:443", connection.Target);
        Assert.Equal("socks", connection.Inbound);
        Assert.Equal("proxy", connection.Outbound);
        Assert.Equal("Matched", connection.Status);
    }

    [Fact]
    public void Attribute_MarksProxyConnectionUnknownWhenNoLogMatches()
    {
        var tcpConnections = new[]
        {
            new TcpConnectionInfo("127.0.0.1", 9852, "127.0.0.1", 10808, 1234, "chrome.exe", "Established")
        };
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808 }
        };

        var result = new AttributionEngine().Attribute(tcpConnections, [], settings);

        var connection = Assert.Single(result);
        Assert.Equal("unknown", connection.Target);
        Assert.Equal("unknown", connection.Inbound);
        Assert.Equal("unknown", connection.Outbound);
        Assert.Equal("PortOnly", connection.Status);
    }

    [Fact]
    public void Attribute_UsesCachedConnectionWhenCurrentConnectionDisappears()
    {
        var now = new DateTime(2026, 6, 13, 16, 40, 0);
        var snapshots = new[]
        {
            new ConnectionSnapshot(
                new TcpConnectionInfo("127.0.0.1", 9852, "127.0.0.1", 10808, 1234, "chrome.exe", "Established"),
                now.AddSeconds(-30),
                false)
        };
        var logRecords = new[]
        {
            new LogConnectionRecord(
                now.AddSeconds(-1),
                "127.0.0.1",
                9852,
                "github.com",
                443,
                "socks",
                "proxy",
                "raw")
        };
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808 }
        };

        var result = new AttributionEngine().Attribute(
            snapshots,
            logRecords,
            settings,
            new Dictionary<TrafficFlowKey, TrafficCounters>(),
            now);

        var connection = Assert.Single(result);
        Assert.Equal("chrome.exe", connection.Application);
        Assert.Equal("Matched", connection.Status);
        Assert.Equal(now.AddSeconds(-30), connection.LastSeen);
    }

    [Fact]
    public void SummarizeApplications_SplitsBytesByOutbound()
    {
        var now = new DateTime(2026, 6, 13, 16, 40, 0);
        var snapshots = new[]
        {
            new ConnectionSnapshot(
                new TcpConnectionInfo("127.0.0.1", 9852, "127.0.0.1", 10808, 1234, "chrome.exe", "Established"),
                now,
                true),
            new ConnectionSnapshot(
                new TcpConnectionInfo("127.0.0.1", 9853, "127.0.0.1", 10808, 1234, "chrome.exe", "Established"),
                now,
                true)
        };
        var logRecords = new[]
        {
            new LogConnectionRecord(now, "127.0.0.1", 9852, "google.com", 443, "socks", "proxy", "raw"),
            new LogConnectionRecord(now, "127.0.0.1", 9853, "example.cn", 443, "socks", "direct", "raw")
        };
        var traffic = new Dictionary<TrafficFlowKey, TrafficCounters>
        {
            [new TrafficFlowKey(1234, "127.0.0.1", 9852, "127.0.0.1", 10808)] = new(100, 20),
            [new TrafficFlowKey(1234, "127.0.0.1", 9853, "127.0.0.1", 10808)] = new(200, 30)
        };
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808 }
        };

        var attributed = new AttributionEngine().Attribute(snapshots, logRecords, settings, traffic, now);
        var summary = Assert.Single(new AttributionEngine().SummarizeApplications(attributed));

        Assert.Equal(350, summary.TotalBytes);
        Assert.Equal(120, summary.ProxyBytes);
        Assert.Equal(230, summary.DirectBytes);
        Assert.Equal(0, summary.UnknownBytes);
    }

    [Fact]
    public void Attribute_RepresentsLogOnlyRowsAsDiagnosticUnknownWithoutPid()
    {
        var now = new DateTime(2026, 6, 13, 16, 40, 0);
        var logRecords = new[]
        {
            new LogConnectionRecord(now, "127.0.0.1", 9852, "github.com", 443, "socks", "proxy", "raw")
        };
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808 }
        };

        var result = new AttributionEngine().Attribute(
            [],
            logRecords,
            settings,
            new Dictionary<TrafficFlowKey, TrafficCounters>(),
            now);

        var connection = Assert.Single(result);
        Assert.Equal("Unknown", connection.Application);
        Assert.Null(connection.ProcessId);
        Assert.Equal("LogOnly", connection.Status);
    }

    [Fact]
    public void SummarizeApplications_ExcludesLogOnlyRows()
    {
        var now = new DateTime(2026, 6, 13, 16, 40, 0);
        var connections = new[]
        {
            new AttributedConnection(now, "chrome.exe", 1234, 9852, "github.com:443", "socks", "proxy", "Matched", 100, 20, now),
            new AttributedConnection(now, "Unknown", null, 9853, "example.com:443", "socks", "proxy", "LogOnly", 0, 0, now)
        };

        var summaries = new AttributionEngine().SummarizeApplications(connections);

        var summary = Assert.Single(summaries);
        Assert.Equal("chrome.exe", summary.Application);
        Assert.Equal(1234, summary.ProcessId);
        Assert.Equal(120, summary.TotalBytes);
    }

    [Fact]
    public void SummarizeApplications_ExcludesPidZeroIdleRows()
    {
        var now = new DateTime(2026, 6, 13, 16, 40, 0);
        var connections = new[]
        {
            new AttributedConnection(now, "chrome.exe", 1234, 9852, "github.com:443", "socks", "proxy", "Matched", 100, 20, now),
            new AttributedConnection(now, "Idle.exe", 0, 9853, "chatgpt.com:443", "socks", "proxy", "Matched", 5000, 500, now)
        };

        var summaries = new AttributionEngine().SummarizeApplications(connections);

        var summary = Assert.Single(summaries);
        Assert.Equal("chrome.exe", summary.Application);
        Assert.Equal(120, summary.TotalBytes);
    }
}

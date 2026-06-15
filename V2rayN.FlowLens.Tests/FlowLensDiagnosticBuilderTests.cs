using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class FlowLensDiagnosticBuilderTests
{
    [Fact]
    public void Build_SummarizesRefreshState()
    {
        var builder = new FlowLensDiagnosticBuilder();
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808, 10809 }
        };
        var configResult = new V2rayNConfigDiscoveryResult(
            V2rayNConfigDiscoveryStatus.Found,
            @"E:\tools\v2rayN",
            new HashSet<int> { 10808 },
            "Read local proxy ports.");
        var tcpConnections = new[]
        {
            new TcpConnectionInfo("127.0.0.1", 50000, "127.0.0.1", 10808, 123, "chrome.exe", "Established"),
            new TcpConnectionInfo("127.0.0.1", 50001, "93.184.216.34", 443, 123, "chrome.exe", "Established")
        };
        var logRecords = new[]
        {
            new LogConnectionRecord(DateTime.Parse("2026-06-14T12:00:00"), "127.0.0.1", 50000, "example.com", 443, "socks", "proxy", "raw")
        };
        var attributed = new[]
        {
            new AttributedConnection(DateTime.Now, "chrome.exe", 123, 50000, "example.com:443", "socks", "proxy", "Matched", 10, 20, DateTime.Now),
            new AttributedConnection(null, "edge.exe", 456, 50002, "unknown", "unknown", "unknown", "PortOnly", 0, 0, DateTime.Now),
            new AttributedConnection(DateTime.Now, "Unknown", null, 50003, "github.com:443", "socks", "proxy", "LogOnly", 0, 0, DateTime.Now),
            new AttributedConnection(null, "unknown", null, 0, "unknown", "unknown", "unknown", "Unknown", 0, 0, DateTime.Now)
        };

        var diagnostics = builder.Build(
            isAdministrator: false,
            etwStatus: "ETW traffic unavailable: run FlowLens as administrator.",
            logHealthStatus: LogHealthStatus.CoreRoutingLogOk,
            configResult,
            settings,
            [@"E:\tools\v2rayN\guiLogs\Vaccess_2026-06-14.txt"],
            tcpConnections,
            logRecords,
            attributed,
            DateTime.Parse("2026-06-14T12:00:01"));

        Assert.False(diagnostics.IsAdministrator);
        Assert.Equal("Found", diagnostics.LogDiscoveryStatus);
        Assert.Equal(LogHealthStatus.CoreRoutingLogOk, diagnostics.LogHealthStatus);
        Assert.Equal(1, diagnostics.ObservedProxyPortConnections);
        Assert.Equal(1, diagnostics.ParsedLogRecordCount);
        Assert.Equal(1, diagnostics.MatchedCount);
        Assert.Equal(1, diagnostics.PortOnlyCount);
        Assert.Equal(1, diagnostics.LogOnlyCount);
        Assert.Equal(1, diagnostics.UnknownCount);
        Assert.Equal("Need admin for ETW", diagnostics.AdminDisplay);
    }
}

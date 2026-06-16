using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class DiagnosticsReportBuilderTests
{
    [Fact]
    public void Build_IncludesOperationalFields()
    {
        var diagnostics = new FlowLensDiagnostics
        {
            IsAdministrator = true,
            EtwStatus = "Running",
            LogDiscoveryStatus = "Found",
            LogHealthStatus = LogHealthStatus.CoreRoutingLogOk,
            ResolvedLogFiles = ["Vaccess_2026-06-16.txt"],
            ConfiguredProxyPorts = new HashSet<int> { 10808 },
            MatchedCount = 2,
            TodayHistory = new TodayHistoryState(new DateOnly(2026, 6, 16), "history.json", "Saved")
        };

        var report = DiagnosticsReportBuilder.Build(diagnostics, "Running", "Enabled", "2026-06-16 09:00:00", "V1.6");

        Assert.Contains("V1.6", report);
        Assert.Contains("Admin: OK", report);
        Assert.Contains("ETW: Running", report);
        Assert.Contains("Proxy ports: 10808", report);
        Assert.Contains("Matched 2", report);
        Assert.Contains("history.json", report);
    }
}

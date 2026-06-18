using System.Text;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public static class DiagnosticsReportBuilder
{
    public static string Build(
        FlowLensDiagnostics diagnostics,
        string refreshState,
        string trayMode,
        string sessionStarted,
        string versionStage,
        bool tunDiagnosticsAvailable = false)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"v2rayN FlowLens diagnostics ({versionStage})");
        builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine($"Admin: {diagnostics.AdminDisplay}");
        builder.AppendLine($"ETW: {diagnostics.EtwStatus}");
        builder.AppendLine($"Access log: {diagnostics.LogDiscoveryStatus}");
        builder.AppendLine($"Log health: {diagnostics.LogHealthStatus}");
        builder.AppendLine($"v2rayN config: {diagnostics.V2rayNConfigStatus}");
        builder.AppendLine($"v2rayN root: {diagnostics.V2rayNRootDirectory}");
        builder.AppendLine($"Config message: {diagnostics.V2rayNConfigMessage}");
        builder.AppendLine($"Proxy ports: {diagnostics.ProxyPortsDisplay}");
        builder.AppendLine($"Active logs: {diagnostics.ActiveLogsDisplay}");
        builder.AppendLine($"Match stats: {diagnostics.MatchStatsDisplay}");
        builder.AppendLine($"Last refresh: {diagnostics.LastRefreshDisplay}");
        builder.AppendLine($"Refresh state: {refreshState}");
        builder.AppendLine($"Tray mode: {trayMode}");
        builder.AppendLine($"Session started: {sessionStarted}");
        builder.AppendLine($"Today history: {diagnostics.TodayHistoryDisplay}");
        builder.AppendLine($"Attribution mode: {diagnostics.AttributionModeDisplay}");
        builder.AppendLine($"TUN evidence: {diagnostics.TunEvidenceDisplay}");
        builder.AppendLine($"TUN diagnostics JSON: {(tunDiagnosticsAvailable ? "available" : "not available")}");
        builder.AppendLine($"Confidence: {diagnostics.ConfidenceStatsDisplay}");
        return builder.ToString();
    }
}

using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class FlowLensDiagnosticBuilder
{
    public FlowLensDiagnostics Build(
        bool isAdministrator,
        string etwStatus,
        LogHealthStatus logHealthStatus,
        V2rayNConfigDiscoveryResult configDiscoveryResult,
        FlowLensSettings settings,
        IReadOnlyList<string> resolvedLogFiles,
        IReadOnlyList<TcpConnectionInfo> tcpConnections,
        IReadOnlyList<LogConnectionRecord> logRecords,
        IReadOnlyList<AttributedConnection> attributedConnections,
        DateTime now,
        TodayHistoryState? todayHistory = null)
    {
        return new FlowLensDiagnostics
        {
            IsAdministrator = isAdministrator,
            EtwStatus = etwStatus,
            LogDiscoveryStatus = resolvedLogFiles.Count > 0 ? "Found" : "Not found",
            LogHealthStatus = logHealthStatus,
            ResolvedLogFiles = resolvedLogFiles,
            ConfiguredProxyPorts = settings.ProxyPorts,
            ObservedProxyPortConnections = tcpConnections.Count(connection =>
                settings.ProxyPorts.Contains(connection.RemotePort) && IsLoopback(connection.RemoteAddress)),
            ParsedLogRecordCount = logRecords.Count,
            MatchedCount = CountStatus(attributedConnections, "Matched"),
            PortOnlyCount = CountStatus(attributedConnections, "PortOnly"),
            LogOnlyCount = CountStatus(attributedConnections, "LogOnly"),
            UnknownCount = CountStatus(attributedConnections, "Unknown"),
            LastRefreshTime = now,
            V2rayNConfigStatus = configDiscoveryResult.Status.ToString(),
            V2rayNRootDirectory = configDiscoveryResult.RootDirectory,
            V2rayNConfigMessage = configDiscoveryResult.Message,
            TodayHistory = todayHistory ?? new TodayHistoryState(DateOnly.FromDateTime(now), string.Empty, "Not loaded")
        };
    }

    private static int CountStatus(IEnumerable<AttributedConnection> connections, string status)
    {
        return connections.Count(connection => connection.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLoopback(string address)
    {
        return address.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }
}

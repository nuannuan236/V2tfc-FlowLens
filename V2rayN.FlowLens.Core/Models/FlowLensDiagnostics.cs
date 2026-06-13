namespace V2rayN.FlowLens.Core.Models;

public sealed record FlowLensDiagnostics
{
    public bool IsAdministrator { get; init; }

    public string EtwStatus { get; init; } = string.Empty;

    public string LogDiscoveryStatus { get; init; } = string.Empty;

    public LogHealthStatus LogHealthStatus { get; init; }

    public IReadOnlyList<string> ResolvedLogFiles { get; init; } = [];

    public IReadOnlySet<int> ConfiguredProxyPorts { get; init; } = new HashSet<int>();

    public int ObservedProxyPortConnections { get; init; }

    public int ParsedLogRecordCount { get; init; }

    public int MatchedCount { get; init; }

    public int PortOnlyCount { get; init; }

    public int LogOnlyCount { get; init; }

    public int UnknownCount { get; init; }

    public DateTime LastRefreshTime { get; init; }

    public string V2rayNConfigStatus { get; init; } = string.Empty;

    public string V2rayNRootDirectory { get; init; } = string.Empty;

    public string V2rayNConfigMessage { get; init; } = string.Empty;

    public string AdminDisplay => IsAdministrator ? "OK" : "Need admin for ETW";

    public string ProxyPortsDisplay => ConfiguredProxyPorts.Count == 0
        ? "none"
        : string.Join(", ", ConfiguredProxyPorts.Order());

    public string ActiveLogsDisplay => ResolvedLogFiles.Count == 0
        ? "none"
        : string.Join("; ", ResolvedLogFiles.Take(5));

    public string MatchStatsDisplay => $"Matched {MatchedCount}, PortOnly {PortOnlyCount}, LogOnly {LogOnlyCount}, Unknown {UnknownCount}";

    public string LastRefreshDisplay => LastRefreshTime == DateTime.MinValue
        ? "never"
        : LastRefreshTime.ToString("yyyy-MM-dd HH:mm:ss");
}

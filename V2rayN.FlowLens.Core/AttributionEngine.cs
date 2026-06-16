using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class AttributionEngine
{
    private static readonly HashSet<string> CoreProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "xray.exe",
        "sing-box.exe",
        "HttpProxy.exe",
        "v2ray.exe",
        "v2rayN.exe"
    };

    public IReadOnlyList<AttributedConnection> Attribute(
        IEnumerable<TcpConnectionInfo> tcpConnections,
        IEnumerable<LogConnectionRecord> logRecords,
        FlowLensSettings settings)
    {
        var now = DateTime.Now;
        var snapshots = tcpConnections.Select(connection => new ConnectionSnapshot(connection, now, true));
        return Attribute(snapshots, logRecords, settings, new Dictionary<TrafficFlowKey, TrafficCounters>(), now);
    }

    public IReadOnlyList<AttributedConnection> Attribute(
        IEnumerable<ConnectionSnapshot> connectionSnapshots,
        IEnumerable<LogConnectionRecord> logRecords,
        FlowLensSettings settings,
        IReadOnlyDictionary<TrafficFlowKey, TrafficCounters> trafficCounters,
        DateTime now)
    {
        var logsBySourcePort = logRecords
            .Where(record => IsLoopback(record.SourceAddress))
            .GroupBy(record => record.SourcePort)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(record => record.Timestamp).First());

        var attributed = new List<AttributedConnection>();
        var matchedSourcePorts = new HashSet<int>();

        foreach (var snapshot in connectionSnapshots)
        {
            var connection = snapshot.Connection;
            if (!settings.ProxyPorts.Contains(connection.RemotePort) || !IsLoopback(connection.RemoteAddress))
            {
                continue;
            }

            if (settings.HideCoreProcesses && CoreProcessNames.Contains(connection.ProcessName))
            {
                continue;
            }

            logsBySourcePort.TryGetValue(connection.LocalPort, out var logRecord);
            var outbound = logRecord?.Outbound ?? "unknown";
            if (settings.OnlyShowProxy && !outbound.Equals("proxy", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (logRecord is not null)
            {
                matchedSourcePorts.Add(logRecord.SourcePort);
            }

            var traffic = GetTraffic(connection, trafficCounters);
            attributed.Add(new AttributedConnection(
                logRecord?.Timestamp,
                connection.ProcessName,
                connection.ProcessId,
                connection.LocalPort,
                FormatTarget(logRecord),
                logRecord?.Inbound ?? "unknown",
                outbound,
                logRecord is null ? "PortOnly" : "Matched",
                traffic.SentBytes,
                traffic.ReceivedBytes,
                snapshot.LastSeen));
        }

        foreach (var logRecord in logRecords.Where(record => IsRecent(record, now) && !matchedSourcePorts.Contains(record.SourcePort)))
        {
            if (settings.OnlyShowProxy && !logRecord.Outbound.Equals("proxy", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            attributed.Add(new AttributedConnection(
                logRecord.Timestamp,
                "Unknown",
                null,
                logRecord.SourcePort,
                FormatTarget(logRecord),
                logRecord.Inbound,
                logRecord.Outbound,
                "LogOnly",
                0,
                0,
                logRecord.Timestamp));
        }

        return attributed
            .OrderByDescending(connection => connection.Timestamp ?? DateTime.MinValue)
            .ThenBy(connection => connection.Application, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<ApplicationTrafficSummary> SummarizeApplications(IEnumerable<AttributedConnection> connections)
    {
        return connections
            .Where(connection => !connection.Status.Equals("LogOnly", StringComparison.OrdinalIgnoreCase))
            .GroupBy(connection => connection.Application, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var processIds = group.Select(connection => connection.ProcessId).Distinct().ToArray();
                return new ApplicationTrafficSummary(
                    group.Key,
                    processIds.Length == 1 ? processIds[0] : null,
                    group.Count(),
                    CountOutbound(group, "proxy"),
                    CountOutbound(group, "direct"),
                    CountOutbound(group, "block"),
                    CountOutbound(group, "unknown"),
                    group.Sum(connection => connection.TotalBytes),
                    SumOutboundBytes(group, "proxy"),
                    SumOutboundBytes(group, "direct"),
                    SumOutboundBytes(group, "unknown"),
                    group.Max(connection => connection.LastSeen));
            })
            .OrderByDescending(summary => summary.TotalBytes)
            .ThenByDescending(summary => summary.ProxyCount)
            .ThenByDescending(summary => summary.ConnectionCount)
            .ThenBy(summary => summary.Application, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<DomainTrafficSummary> SummarizeDomains(IEnumerable<AttributedConnection> connections)
    {
        return connections
            .Where(connection => !string.IsNullOrWhiteSpace(connection.Target) && connection.Target != "unknown")
            .GroupBy(connection => ExtractDomain(connection.Target), StringComparer.OrdinalIgnoreCase)
            .Select(group => new DomainTrafficSummary(
                group.Key,
                group.Count(),
                string.Join(", ", group.Select(connection => connection.Application).Distinct(StringComparer.OrdinalIgnoreCase).Order()),
                CountOutbound(group, "proxy"),
                CountOutbound(group, "direct"),
                CountOutbound(group, "unknown"),
                group.Sum(connection => connection.TotalBytes),
                SumOutboundBytes(group, "proxy"),
                SumOutboundBytes(group, "direct"),
                SumOutboundBytes(group, "unknown"),
                group.Max(connection => connection.LastSeen)))
            .OrderByDescending(summary => summary.TotalBytes)
            .ThenByDescending(summary => summary.ConnectionCount)
            .ThenBy(summary => summary.Domain, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int CountOutbound(IEnumerable<AttributedConnection> connections, string outbound)
    {
        return connections.Count(connection => connection.Outbound.Equals(outbound, StringComparison.OrdinalIgnoreCase));
    }

    private static long SumOutboundBytes(IEnumerable<AttributedConnection> connections, string outbound)
    {
        return connections
            .Where(connection => connection.Outbound.Equals(outbound, StringComparison.OrdinalIgnoreCase))
            .Sum(connection => connection.TotalBytes);
    }

    private static TrafficCounters GetTraffic(
        TcpConnectionInfo connection,
        IReadOnlyDictionary<TrafficFlowKey, TrafficCounters> trafficCounters)
    {
        var key = new TrafficFlowKey(
            connection.ProcessId,
            connection.LocalAddress,
            connection.LocalPort,
            connection.RemoteAddress,
            connection.RemotePort);

        return trafficCounters.TryGetValue(key, out var traffic)
            ? traffic
            : new TrafficCounters(0, 0);
    }

    private static bool IsRecent(LogConnectionRecord record, DateTime now)
    {
        if (record.Timestamp == DateTime.MinValue)
        {
            return false;
        }

        return now - record.Timestamp <= TimeSpan.FromSeconds(120);
    }

    private static bool IsLoopback(string address)
    {
        return address.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatTarget(LogConnectionRecord? record)
    {
        if (record is null)
        {
            return "unknown";
        }

        return record.TargetPort is null ? record.TargetHost : $"{record.TargetHost}:{record.TargetPort}";
    }

    private static string ExtractDomain(string target)
    {
        var separatorIndex = target.LastIndexOf(':');
        if (separatorIndex > 0 && separatorIndex < target.Length - 1 && int.TryParse(target[(separatorIndex + 1)..], out _))
        {
            return target[..separatorIndex];
        }

        return target;
    }
}

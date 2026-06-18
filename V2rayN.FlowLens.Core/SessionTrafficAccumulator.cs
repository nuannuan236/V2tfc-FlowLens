using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class SessionTrafficAccumulator
{
    private readonly Dictionary<SessionConnectionKey, SessionConnectionState> connections = [];

    public DateTime StartedAt { get; private set; } = DateTime.Now;

    public void AddSnapshot(IEnumerable<AttributedConnection> attributedConnections)
    {
        foreach (var connection in attributedConnections.Where(ShouldAccumulate))
        {
            var key = SessionConnectionKey.From(connection);
            if (!connections.TryGetValue(key, out var state))
            {
                connections[key] = SessionConnectionState.Create(connection);
                continue;
            }

            connections[key] = state.Update(connection);
        }
    }

    public IReadOnlyList<ApplicationTrafficSummary> GetApplicationSummaries()
    {
        return connections.Values
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
                    group.Sum(connection => connection.SessionBytes),
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

    public IReadOnlyList<DomainTrafficSummary> GetDomainSummaries()
    {
        return connections.Values
            .Where(connection => !string.IsNullOrWhiteSpace(connection.Target) && connection.Target != "unknown")
            .GroupBy(connection => ExtractDomain(connection.Target), StringComparer.OrdinalIgnoreCase)
            .Select(group => new DomainTrafficSummary(
                group.Key,
                group.Count(),
                string.Join(", ", group.Select(connection => connection.Application).Distinct(StringComparer.OrdinalIgnoreCase).Order()),
                CountOutbound(group, "proxy"),
                CountOutbound(group, "direct"),
                CountOutbound(group, "unknown"),
                group.Sum(connection => connection.SessionBytes),
                SumOutboundBytes(group, "proxy"),
                SumOutboundBytes(group, "direct"),
                SumOutboundBytes(group, "unknown"),
                group.Max(connection => connection.LastSeen)))
            .OrderByDescending(summary => summary.TotalBytes)
            .ThenByDescending(summary => summary.ConnectionCount)
            .ThenBy(summary => summary.Domain, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void Reset()
    {
        connections.Clear();
        StartedAt = DateTime.Now;
    }

    private static bool ShouldAccumulate(AttributedConnection connection)
    {
        return AttributionCountingPolicy.CountsAsApplicationTraffic(connection);
    }

    private static int CountOutbound(IEnumerable<SessionConnectionState> connections, string outbound)
    {
        return connections.Count(connection => connection.Outbound.Equals(outbound, StringComparison.OrdinalIgnoreCase));
    }

    private static long SumOutboundBytes(IEnumerable<SessionConnectionState> connections, string outbound)
    {
        return connections
            .Where(connection => connection.Outbound.Equals(outbound, StringComparison.OrdinalIgnoreCase))
            .Sum(connection => connection.SessionBytes);
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

    private sealed record SessionConnectionKey(
        string Application,
        int ProcessId,
        int SourcePort,
        string Target,
        string Inbound,
        string Outbound,
        long TimestampTicks)
    {
        public static SessionConnectionKey From(AttributedConnection connection)
        {
            return new SessionConnectionKey(
                connection.Application,
                connection.ProcessId ?? 0,
                connection.SourcePort,
                connection.Target,
                connection.Inbound,
                connection.Outbound,
                connection.Timestamp?.Ticks ?? 0);
        }
    }

    private sealed record SessionConnectionState(
        string Application,
        int ProcessId,
        string Target,
        string Outbound,
        long LastObservedBytes,
        long SessionBytes,
        DateTime LastSeen)
    {
        public static SessionConnectionState Create(AttributedConnection connection)
        {
            return new SessionConnectionState(
                connection.Application,
                connection.ProcessId ?? 0,
                connection.Target,
                connection.Outbound,
                connection.TotalBytes,
                connection.TotalBytes,
                connection.LastSeen);
        }

        public SessionConnectionState Update(AttributedConnection connection)
        {
            var delta = Math.Max(0, connection.TotalBytes - LastObservedBytes);
            return this with
            {
                LastObservedBytes = Math.Max(LastObservedBytes, connection.TotalBytes),
                SessionBytes = SessionBytes + delta,
                LastSeen = connection.LastSeen > LastSeen ? connection.LastSeen : LastSeen
            };
        }
    }
}

using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class TodayTrafficAccumulator
{
    private readonly Dictionary<TodayConnectionKey, TodayConnectionState> connections = [];
    private readonly Dictionary<string, TodayApplicationAggregate> applications = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TodayDomainAggregate> domains = new(StringComparer.OrdinalIgnoreCase);

    public DateOnly Date { get; private set; } = DateOnly.FromDateTime(DateTime.Now);

    public void Load(TodayTrafficHistory history)
    {
        Date = history.Date;
        connections.Clear();
        applications.Clear();
        domains.Clear();

        foreach (var row in history.Applications)
        {
            applications[row.Application] = TodayApplicationAggregate.FromSummary(row);
        }

        foreach (var row in history.Domains)
        {
            domains[row.Domain] = TodayDomainAggregate.FromSummary(row);
        }
    }

    public TodayTrafficHistory ToHistory()
    {
        return new TodayTrafficHistory(Date, GetApplicationSummaries(), GetDomainSummaries());
    }

    public void AddSnapshot(IEnumerable<AttributedConnection> attributedConnections)
    {
        foreach (var connection in attributedConnections.Where(ShouldAccumulate))
        {
            var key = TodayConnectionKey.From(connection);
            var isNewConnection = !connections.TryGetValue(key, out var state);
            var delta = isNewConnection ? connection.TotalBytes : Math.Max(0, connection.TotalBytes - state!.LastObservedBytes);
            var lastSeen = isNewConnection || connection.LastSeen > state!.LastSeen ? connection.LastSeen : state.LastSeen;

            connections[key] = new TodayConnectionState(Math.Max(isNewConnection ? 0 : state!.LastObservedBytes, connection.TotalBytes), lastSeen);
            AddApplication(connection, delta, isNewConnection, lastSeen);
            AddDomain(connection, delta, isNewConnection, lastSeen);
        }
    }

    public IReadOnlyList<ApplicationTrafficSummary> GetApplicationSummaries()
    {
        return applications.Values
            .Select(aggregate => aggregate.ToSummary())
            .OrderByDescending(summary => summary.TotalBytes)
            .ThenByDescending(summary => summary.ProxyCount)
            .ThenByDescending(summary => summary.ConnectionCount)
            .ThenBy(summary => summary.Application, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<DomainTrafficSummary> GetDomainSummaries()
    {
        return domains.Values
            .Select(aggregate => aggregate.ToSummary())
            .OrderByDescending(summary => summary.TotalBytes)
            .ThenByDescending(summary => summary.ConnectionCount)
            .ThenBy(summary => summary.Domain, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private void AddApplication(AttributedConnection connection, long byteDelta, bool isNewConnection, DateTime lastSeen)
    {
        if (!applications.TryGetValue(connection.Application, out var aggregate))
        {
            aggregate = TodayApplicationAggregate.Create(connection.Application);
            applications[connection.Application] = aggregate;
        }

        aggregate.Add(connection, byteDelta, isNewConnection, lastSeen);
    }

    private void AddDomain(AttributedConnection connection, long byteDelta, bool isNewConnection, DateTime lastSeen)
    {
        if (string.IsNullOrWhiteSpace(connection.Target) || connection.Target.Equals("unknown", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var domain = ExtractDomain(connection.Target);
        if (!domains.TryGetValue(domain, out var aggregate))
        {
            aggregate = TodayDomainAggregate.Create(domain);
            domains[domain] = aggregate;
        }

        aggregate.Add(connection, byteDelta, isNewConnection, lastSeen);
    }

    private static bool ShouldAccumulate(AttributedConnection connection)
    {
        return AttributionCountingPolicy.CountsAsApplicationTraffic(connection);
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

    private sealed record TodayConnectionKey(
        string Application,
        int ProcessId,
        int SourcePort,
        string Target,
        string Inbound,
        string Outbound,
        long TimestampTicks)
    {
        public static TodayConnectionKey From(AttributedConnection connection)
        {
            return new TodayConnectionKey(
                connection.Application,
                connection.ProcessId ?? 0,
                connection.SourcePort,
                connection.Target,
                connection.Inbound,
                connection.Outbound,
                connection.Timestamp?.Ticks ?? 0);
        }
    }

    private sealed record TodayConnectionState(long LastObservedBytes, DateTime LastSeen);

    private sealed class TodayApplicationAggregate
    {
        private readonly HashSet<int> processIds = [];

        private TodayApplicationAggregate(string application)
        {
            Application = application;
        }

        public string Application { get; }

        public int ConnectionCount { get; private set; }

        public int ProxyCount { get; private set; }

        public int DirectCount { get; private set; }

        public int BlockCount { get; private set; }

        public int UnknownCount { get; private set; }

        public long TotalBytes { get; private set; }

        public long ProxyBytes { get; private set; }

        public long DirectBytes { get; private set; }

        public long UnknownBytes { get; private set; }

        public DateTime LastSeen { get; private set; }

        public static TodayApplicationAggregate Create(string application)
        {
            return new TodayApplicationAggregate(application);
        }

        public static TodayApplicationAggregate FromSummary(ApplicationTrafficSummary summary)
        {
            var aggregate = new TodayApplicationAggregate(summary.Application)
            {
                ConnectionCount = summary.ConnectionCount,
                ProxyCount = summary.ProxyCount,
                DirectCount = summary.DirectCount,
                BlockCount = summary.BlockCount,
                UnknownCount = summary.UnknownCount,
                TotalBytes = summary.TotalBytes,
                ProxyBytes = summary.ProxyBytes,
                DirectBytes = summary.DirectBytes,
                UnknownBytes = summary.UnknownBytes,
                LastSeen = summary.LastSeen
            };

            if (summary.ProcessId is not null)
            {
                aggregate.processIds.Add(summary.ProcessId.Value);
            }

            return aggregate;
        }

        public void Add(AttributedConnection connection, long byteDelta, bool isNewConnection, DateTime lastSeen)
        {
            if (connection.ProcessId is not null)
            {
                processIds.Add(connection.ProcessId.Value);
            }

            if (isNewConnection)
            {
                ConnectionCount++;
                IncrementOutboundCount(connection.Outbound);
            }

            AddBytes(connection.Outbound, byteDelta);
            if (lastSeen > LastSeen)
            {
                LastSeen = lastSeen;
            }
        }

        public ApplicationTrafficSummary ToSummary()
        {
            return new ApplicationTrafficSummary(
                Application,
                processIds.Count == 1 ? processIds.Single() : null,
                ConnectionCount,
                ProxyCount,
                DirectCount,
                BlockCount,
                UnknownCount,
                TotalBytes,
                ProxyBytes,
                DirectBytes,
                UnknownBytes,
                LastSeen);
        }

        private void IncrementOutboundCount(string outbound)
        {
            if (outbound.Equals("proxy", StringComparison.OrdinalIgnoreCase))
            {
                ProxyCount++;
            }
            else if (outbound.Equals("direct", StringComparison.OrdinalIgnoreCase))
            {
                DirectCount++;
            }
            else if (outbound.Equals("block", StringComparison.OrdinalIgnoreCase))
            {
                BlockCount++;
            }
            else
            {
                UnknownCount++;
            }
        }

        private void AddBytes(string outbound, long bytes)
        {
            TotalBytes += bytes;
            if (outbound.Equals("proxy", StringComparison.OrdinalIgnoreCase))
            {
                ProxyBytes += bytes;
            }
            else if (outbound.Equals("direct", StringComparison.OrdinalIgnoreCase))
            {
                DirectBytes += bytes;
            }
            else
            {
                UnknownBytes += bytes;
            }
        }
    }

    private sealed class TodayDomainAggregate
    {
        private readonly HashSet<string> applications = new(StringComparer.OrdinalIgnoreCase);

        private TodayDomainAggregate(string domain)
        {
            Domain = domain;
        }

        public string Domain { get; }

        public int ConnectionCount { get; private set; }

        public int ProxyCount { get; private set; }

        public int DirectCount { get; private set; }

        public int UnknownCount { get; private set; }

        public long TotalBytes { get; private set; }

        public long ProxyBytes { get; private set; }

        public long DirectBytes { get; private set; }

        public long UnknownBytes { get; private set; }

        public DateTime LastSeen { get; private set; }

        public static TodayDomainAggregate Create(string domain)
        {
            return new TodayDomainAggregate(domain);
        }

        public static TodayDomainAggregate FromSummary(DomainTrafficSummary summary)
        {
            var aggregate = new TodayDomainAggregate(summary.Domain)
            {
                ConnectionCount = summary.ConnectionCount,
                ProxyCount = summary.ProxyCount,
                DirectCount = summary.DirectCount,
                UnknownCount = summary.UnknownCount,
                TotalBytes = summary.TotalBytes,
                ProxyBytes = summary.ProxyBytes,
                DirectBytes = summary.DirectBytes,
                UnknownBytes = summary.UnknownBytes,
                LastSeen = summary.LastSeen
            };

            foreach (var application in summary.Applications.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                aggregate.applications.Add(application);
            }

            return aggregate;
        }

        public void Add(AttributedConnection connection, long byteDelta, bool isNewConnection, DateTime lastSeen)
        {
            applications.Add(connection.Application);
            if (isNewConnection)
            {
                ConnectionCount++;
                IncrementOutboundCount(connection.Outbound);
            }

            AddBytes(connection.Outbound, byteDelta);
            if (lastSeen > LastSeen)
            {
                LastSeen = lastSeen;
            }
        }

        public DomainTrafficSummary ToSummary()
        {
            return new DomainTrafficSummary(
                Domain,
                ConnectionCount,
                string.Join(", ", applications.Order()),
                ProxyCount,
                DirectCount,
                UnknownCount,
                TotalBytes,
                ProxyBytes,
                DirectBytes,
                UnknownBytes,
                LastSeen);
        }

        private void IncrementOutboundCount(string outbound)
        {
            if (outbound.Equals("proxy", StringComparison.OrdinalIgnoreCase))
            {
                ProxyCount++;
            }
            else if (outbound.Equals("direct", StringComparison.OrdinalIgnoreCase))
            {
                DirectCount++;
            }
            else
            {
                UnknownCount++;
            }
        }

        private void AddBytes(string outbound, long bytes)
        {
            TotalBytes += bytes;
            if (outbound.Equals("proxy", StringComparison.OrdinalIgnoreCase))
            {
                ProxyBytes += bytes;
            }
            else if (outbound.Equals("direct", StringComparison.OrdinalIgnoreCase))
            {
                DirectBytes += bytes;
            }
            else
            {
                UnknownBytes += bytes;
            }
        }
    }
}

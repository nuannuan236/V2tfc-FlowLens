using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class TunAttributionEngine
{
    public const int MatchWindowSeconds = 5;

    private static readonly TimeSpan MatchWindow = TimeSpan.FromSeconds(MatchWindowSeconds);

    private static readonly HashSet<string> CoreProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "xray.exe",
        "sing-box.exe",
        "HttpProxy.exe",
        "v2ray.exe",
        "v2rayN.exe"
    };

    public IReadOnlyList<AttributedConnection> Attribute(
        IEnumerable<ConnectionSnapshot> connectionSnapshots,
        IEnumerable<LogConnectionRecord> logRecords,
        FlowLensSettings settings,
        IReadOnlyDictionary<TrafficFlowKey, TrafficCounters> trafficCounters,
        DateTime now)
    {
        var candidates = connectionSnapshots
            .Select(snapshot => CreateCandidate(snapshot, trafficCounters))
            .Where(candidate => !IsLoopback(candidate.RemoteAddress))
            .Where(candidate => !settings.HideCoreProcesses || !CoreProcessNames.Contains(candidate.Application))
            .ToArray();
        var routeEvidence = logRecords
            .Where(record => IsRecent(record, now))
            .Select(ToRouteEvidence)
            .OrderByDescending(evidence => evidence.Timestamp)
            .ToArray();

        if (routeEvidence.Length == 0)
        {
            return candidates
                .Select(candidate => CreateUnknownCandidate(candidate, "No route log evidence in the TUN matching window."))
                .OrderByDescending(connection => connection.LastSeen)
                .ThenBy(connection => connection.Application, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        var attributed = new List<AttributedConnection>();
        var matchedCandidateKeys = new HashSet<TunCandidateKey>();

        foreach (var evidence in routeEvidence)
        {
            var exactCandidates = candidates
                .Where(candidate => IsInWindow(candidate, evidence))
                .Where(candidate => evidence.TargetPort is null || candidate.RemotePort == evidence.TargetPort)
                .Where(candidate => candidate.RemoteAddress.Equals(evidence.TargetHost, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (exactCandidates.Length == 1)
            {
                var candidate = exactCandidates[0];
                matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                attributed.Add(CreateAttributed(candidate, evidence, AttributionConfidence.Matched, "Target IP and port matched route evidence within +/-5 seconds."));
                continue;
            }

            if (exactCandidates.Length > 1)
            {
                attributed.Add(CreateAmbiguous(evidence, exactCandidates, "Multiple processes matched the same target IP and port within +/-5 seconds."));
                foreach (var candidate in exactCandidates)
                {
                    matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                }

                continue;
            }

            if (LooksLikeIpAddress(evidence.TargetHost))
            {
                attributed.Add(CreateUnknownEvidence(evidence, "Route evidence has an IP target, but no TCP candidate matched the target and port."));
                continue;
            }

            var probableCandidates = candidates
                .Where(candidate => IsInWindow(candidate, evidence))
                .Where(candidate => evidence.TargetPort is null || candidate.RemotePort == evidence.TargetPort)
                .ToArray();

            if (probableCandidates.Length == 1)
            {
                var candidate = probableCandidates[0];
                matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                attributed.Add(CreateAttributed(candidate, evidence, AttributionConfidence.Probable, "Domain route evidence cannot be mapped to remote IP; one process candidate matched by time window and port."));
                continue;
            }

            if (probableCandidates.Length > 1)
            {
                attributed.Add(CreateAmbiguous(evidence, probableCandidates, "Domain route evidence matched multiple process candidates by time window and port."));
                foreach (var candidate in probableCandidates)
                {
                    matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                }

                continue;
            }

            attributed.Add(CreateUnknownEvidence(evidence, "Domain route evidence had no process candidate in the TUN matching window."));
        }

        attributed.AddRange(candidates
            .Where(candidate => !matchedCandidateKeys.Contains(TunCandidateKey.From(candidate)))
            .Select(candidate => CreateUnknownCandidate(candidate, "TCP candidate has no route evidence in the TUN matching window.")));

        return attributed
            .OrderByDescending(connection => connection.Timestamp ?? connection.LastSeen)
            .ThenBy(connection => connection.Application, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static TunAttributionCandidate CreateCandidate(
        ConnectionSnapshot snapshot,
        IReadOnlyDictionary<TrafficFlowKey, TrafficCounters> trafficCounters)
    {
        var connection = snapshot.Connection;
        var key = new TrafficFlowKey(
            connection.ProcessId,
            connection.LocalAddress,
            connection.LocalPort,
            connection.RemoteAddress,
            connection.RemotePort);
        var counters = trafficCounters.TryGetValue(key, out var value)
            ? value
            : new TrafficCounters(0, 0);

        return new TunAttributionCandidate(
            connection.ProcessName,
            connection.ProcessId,
            connection.LocalAddress,
            connection.LocalPort,
            connection.RemoteAddress,
            connection.RemotePort,
            snapshot.LastSeen,
            counters.SentBytes,
            counters.ReceivedBytes);
    }

    private static TunRouteEvidence ToRouteEvidence(LogConnectionRecord record)
    {
        return new TunRouteEvidence(
            record.Timestamp,
            record.TargetHost,
            record.TargetPort,
            record.Inbound,
            record.Outbound,
            record.RawLine);
    }

    private static AttributedConnection CreateAttributed(
        TunAttributionCandidate candidate,
        TunRouteEvidence evidence,
        AttributionConfidence confidence,
        string reason)
    {
        return new AttributedConnection(
            evidence.Timestamp,
            candidate.Application,
            candidate.ProcessId,
            candidate.LocalPort,
            FormatTarget(evidence),
            evidence.Inbound,
            evidence.Outbound,
            confidence.ToString(),
            candidate.SentBytes,
            candidate.ReceivedBytes,
            candidate.LastSeen,
            AttributionMode.Tun,
            confidence,
            reason);
    }

    private static AttributedConnection CreateAmbiguous(
        TunRouteEvidence evidence,
        IReadOnlyCollection<TunAttributionCandidate> candidates,
        string reason)
    {
        var applications = string.Join(", ", candidates
            .Select(candidate => $"{candidate.Application}({candidate.ProcessId})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order());
        var totalSent = candidates.Sum(candidate => candidate.SentBytes);
        var totalReceived = candidates.Sum(candidate => candidate.ReceivedBytes);
        var lastSeen = candidates.Count == 0 ? evidence.Timestamp : candidates.Max(candidate => candidate.LastSeen);

        return new AttributedConnection(
            evidence.Timestamp,
            "Unknown",
            null,
            0,
            FormatTarget(evidence),
            evidence.Inbound,
            evidence.Outbound,
            AttributionConfidence.Ambiguous.ToString(),
            totalSent,
            totalReceived,
            lastSeen,
            AttributionMode.Tun,
            AttributionConfidence.Ambiguous,
            $"{reason} Candidates: {applications}");
    }

    private static AttributedConnection CreateUnknownEvidence(TunRouteEvidence evidence, string reason)
    {
        return new AttributedConnection(
            evidence.Timestamp,
            "Unknown",
            null,
            0,
            FormatTarget(evidence),
            evidence.Inbound,
            evidence.Outbound,
            AttributionConfidence.Unknown.ToString(),
            0,
            0,
            evidence.Timestamp,
            AttributionMode.Tun,
            AttributionConfidence.Unknown,
            reason);
    }

    private static AttributedConnection CreateUnknownCandidate(TunAttributionCandidate candidate, string reason)
    {
        return new AttributedConnection(
            null,
            candidate.Application,
            candidate.ProcessId,
            candidate.LocalPort,
            $"{candidate.RemoteAddress}:{candidate.RemotePort}",
            "unknown",
            "unknown",
            AttributionConfidence.Unknown.ToString(),
            candidate.SentBytes,
            candidate.ReceivedBytes,
            candidate.LastSeen,
            AttributionMode.Tun,
            AttributionConfidence.Unknown,
            reason);
    }

    private static bool IsInWindow(TunAttributionCandidate candidate, TunRouteEvidence evidence)
    {
        return Duration(candidate.LastSeen, evidence.Timestamp) <= MatchWindow;
    }

    private static bool IsRecent(LogConnectionRecord record, DateTime now)
    {
        return record.Timestamp != DateTime.MinValue && now - record.Timestamp <= TimeSpan.FromSeconds(120);
    }

    private static TimeSpan Duration(DateTime left, DateTime right)
    {
        return left >= right ? left - right : right - left;
    }

    private static string FormatTarget(TunRouteEvidence evidence)
    {
        return evidence.TargetPort is null ? evidence.TargetHost : $"{evidence.TargetHost}:{evidence.TargetPort}";
    }

    private static bool IsLoopback(string address)
    {
        return address.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeIpAddress(string value)
    {
        return value.Count(character => character == '.') == 3 || value.Contains(':', StringComparison.Ordinal);
    }

    private sealed record TunCandidateKey(int ProcessId, string LocalAddress, int LocalPort, string RemoteAddress, int RemotePort)
    {
        public static TunCandidateKey From(TunAttributionCandidate candidate)
        {
            return new TunCandidateKey(candidate.ProcessId, candidate.LocalAddress, candidate.LocalPort, candidate.RemoteAddress, candidate.RemotePort);
        }
    }
}

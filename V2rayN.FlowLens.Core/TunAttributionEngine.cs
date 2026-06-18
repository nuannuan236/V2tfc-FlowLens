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
        return AttributeWithDiagnostics(connectionSnapshots, logRecords, settings, trafficCounters, now).Connections;
    }

    public TunAttributionResult AttributeWithDiagnostics(
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
        var decisions = new List<TunMatchDecision>();

        if (routeEvidence.Length == 0)
        {
            var unknownRows = candidates
                .Select(candidate => CreateUnknownCandidate(candidate, "No route log evidence in the TUN matching window."))
                .OrderByDescending(connection => connection.LastSeen)
                .ThenBy(connection => connection.Application, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            decisions.Add(new TunMatchDecision(
                "",
                "unknown",
                AttributionConfidence.Unknown,
                "NoRouteEvidence",
                "No route log evidence in the TUN matching window.",
                0,
                0,
                0,
                0,
                candidates.Select(FormatCandidateRef).ToArray()));

            return new TunAttributionResult(
                ApplyVisibilityFilters(unknownRows, settings),
                CreateDiagnostics(now, candidates, routeEvidence, decisions, unknownRows));
        }

        var attributed = new List<AttributedConnection>();
        var matchedCandidateKeys = new HashSet<TunCandidateKey>();

        foreach (var evidence in routeEvidence)
        {
            var availableCandidates = candidates
                .Where(candidate => !matchedCandidateKeys.Contains(TunCandidateKey.From(candidate)))
                .ToArray();

            var exactCandidates = candidates
                .Where(candidate => IsInWindow(candidate, evidence))
                .Where(candidate => evidence.TargetPort is null || candidate.RemotePort == evidence.TargetPort)
                .Where(candidate => candidate.RemoteAddress.Equals(evidence.TargetHost, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var availableExactCandidates = exactCandidates
                .Where(candidate => !matchedCandidateKeys.Contains(TunCandidateKey.From(candidate)))
                .ToArray();
            var consumedExactCandidates = exactCandidates
                .Where(candidate => matchedCandidateKeys.Contains(TunCandidateKey.From(candidate)))
                .ToArray();

            if (availableExactCandidates.Length == 1)
            {
                var candidate = availableExactCandidates[0];
                matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                const string reason = "Target IP and port matched route evidence within +/-5 seconds.";
                attributed.Add(CreateAttributed(candidate, evidence, AttributionConfidence.Matched, reason));
                decisions.Add(CreateDecision(evidence, AttributionConfidence.Matched, "Matched", reason, exactCandidates, availableExactCandidates, [], consumedExactCandidates, [candidate]));
                continue;
            }

            if (availableExactCandidates.Length > 1)
            {
                const string reason = "Multiple processes matched the same target IP and port within +/-5 seconds.";
                attributed.Add(CreateAmbiguous(evidence, availableExactCandidates, reason));
                decisions.Add(CreateDecision(evidence, AttributionConfidence.Ambiguous, "Ambiguous", reason, exactCandidates, availableExactCandidates, [], consumedExactCandidates, availableExactCandidates));
                foreach (var candidate in availableExactCandidates)
                {
                    matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                }

                continue;
            }

            if (exactCandidates.Length > 0)
            {
                decisions.Add(CreateDecision(
                    evidence,
                    AttributionConfidence.Unknown,
                    "SkippedDuplicateExactEvidence",
                    "Route evidence only matched already consumed exact candidates.",
                    exactCandidates,
                    availableExactCandidates,
                    [],
                    consumedExactCandidates,
                    consumedExactCandidates));
                continue;
            }

            if (LooksLikeIpAddress(evidence.TargetHost))
            {
                const string reason = "Route evidence has an IP target, but no TCP candidate matched the target and port.";
                attributed.Add(CreateUnknownEvidence(evidence, reason));
                decisions.Add(CreateDecision(evidence, AttributionConfidence.Unknown, "Unknown", reason, exactCandidates, availableExactCandidates, [], consumedExactCandidates, []));
                continue;
            }

            var probableCandidates = availableCandidates
                .Where(candidate => IsInWindow(candidate, evidence))
                .Where(candidate => evidence.TargetPort is null || candidate.RemotePort == evidence.TargetPort)
                .ToArray();
            var consumedProbableCandidates = candidates
                .Where(candidate => matchedCandidateKeys.Contains(TunCandidateKey.From(candidate)))
                .Where(candidate => IsInWindow(candidate, evidence))
                .Where(candidate => evidence.TargetPort is null || candidate.RemotePort == evidence.TargetPort)
                .ToArray();

            if (probableCandidates.Length == 1)
            {
                var candidate = probableCandidates[0];
                matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                const string reason = "Domain route evidence cannot be mapped to remote IP; one process candidate matched by time window and port.";
                attributed.Add(CreateAttributed(candidate, evidence, AttributionConfidence.Probable, reason));
                decisions.Add(CreateDecision(evidence, AttributionConfidence.Probable, "Probable", reason, exactCandidates, availableExactCandidates, probableCandidates, consumedProbableCandidates, [candidate]));
                continue;
            }

            if (probableCandidates.Length > 1)
            {
                const string reason = "Domain route evidence matched multiple process candidates by time window and port.";
                attributed.Add(CreateAmbiguous(evidence, probableCandidates, reason));
                decisions.Add(CreateDecision(evidence, AttributionConfidence.Ambiguous, "Ambiguous", reason, exactCandidates, availableExactCandidates, probableCandidates, consumedProbableCandidates, probableCandidates));
                foreach (var candidate in probableCandidates)
                {
                    matchedCandidateKeys.Add(TunCandidateKey.From(candidate));
                }

                continue;
            }

            if (consumedProbableCandidates.Length > 0)
            {
                decisions.Add(CreateDecision(
                    evidence,
                    AttributionConfidence.Unknown,
                    "SkippedDuplicateProbableEvidence",
                    "Domain route evidence only matched already consumed candidates by time window and port.",
                    exactCandidates,
                    availableExactCandidates,
                    probableCandidates,
                    consumedProbableCandidates,
                    consumedProbableCandidates));
                continue;
            }

            const string unknownDomainReason = "Domain route evidence had no process candidate in the TUN matching window.";
            attributed.Add(CreateUnknownEvidence(evidence, unknownDomainReason));
            decisions.Add(CreateDecision(evidence, AttributionConfidence.Unknown, "Unknown", unknownDomainReason, exactCandidates, availableExactCandidates, probableCandidates, consumedProbableCandidates, []));
        }

        attributed.AddRange(candidates
            .Where(candidate => !matchedCandidateKeys.Contains(TunCandidateKey.From(candidate)))
            .Select(candidate => CreateUnknownCandidate(candidate, "TCP candidate has no route evidence in the TUN matching window.")));

        var connections = ApplyVisibilityFilters(attributed, settings)
            .OrderByDescending(connection => connection.Timestamp ?? connection.LastSeen)
            .ThenBy(connection => connection.Application, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TunAttributionResult(
            connections,
            CreateDiagnostics(now, candidates, routeEvidence, decisions, attributed));
    }

    private static IReadOnlyList<AttributedConnection> ApplyVisibilityFilters(
        IEnumerable<AttributedConnection> rows,
        FlowLensSettings settings)
    {
        return rows
            .Where(connection => !settings.OnlyShowProxy || connection.Outbound.Equals("proxy", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static TunAttributionDiagnostics CreateDiagnostics(
        DateTime now,
        IReadOnlyCollection<TunAttributionCandidate> candidates,
        IReadOnlyCollection<TunRouteEvidence> routeEvidence,
        IReadOnlyCollection<TunMatchDecision> decisions,
        IEnumerable<AttributedConnection> unfilteredConnections)
    {
        var connections = unfilteredConnections.ToArray();
        return new TunAttributionDiagnostics(
            now,
            MatchWindowSeconds,
            candidates.Select(ToCandidateDiagnostic).ToArray(),
            routeEvidence.Select(ToRouteEvidenceDiagnostic).ToArray(),
            decisions.ToArray(),
            connections.Count(connection => connection.Confidence == AttributionConfidence.Matched),
            connections.Count(connection => connection.Confidence == AttributionConfidence.Probable),
            connections.Count(connection => connection.Confidence == AttributionConfidence.Ambiguous),
            connections.Count(connection => connection.Confidence == AttributionConfidence.Unknown));
    }

    private static TunCandidateDiagnostic ToCandidateDiagnostic(TunAttributionCandidate candidate)
    {
        return new TunCandidateDiagnostic(
            candidate.Application,
            candidate.ProcessId,
            candidate.LocalAddress,
            candidate.LocalPort,
            candidate.RemoteAddress,
            candidate.RemotePort,
            candidate.LastSeen,
            candidate.SentBytes,
            candidate.ReceivedBytes,
            candidate.ProcessId == 0
                || candidate.Application.Equals("Idle", StringComparison.OrdinalIgnoreCase)
                || candidate.Application.Equals("Idle.exe", StringComparison.OrdinalIgnoreCase));
    }

    private static TunRouteEvidenceDiagnostic ToRouteEvidenceDiagnostic(TunRouteEvidence evidence)
    {
        return new TunRouteEvidenceDiagnostic(
            evidence.Timestamp,
            evidence.TargetHost,
            evidence.TargetPort,
            evidence.Inbound,
            evidence.Outbound,
            evidence.RawLine);
    }

    private static TunMatchDecision CreateDecision(
        TunRouteEvidence evidence,
        AttributionConfidence confidence,
        string result,
        string reason,
        IReadOnlyCollection<TunAttributionCandidate> exactCandidates,
        IReadOnlyCollection<TunAttributionCandidate> availableExactCandidates,
        IReadOnlyCollection<TunAttributionCandidate> probableCandidates,
        IReadOnlyCollection<TunAttributionCandidate> consumedCandidates,
        IReadOnlyCollection<TunAttributionCandidate> candidateRefs)
    {
        return new TunMatchDecision(
            FormatTarget(evidence),
            evidence.Outbound,
            confidence,
            result,
            reason,
            exactCandidates.Count,
            availableExactCandidates.Count,
            probableCandidates.Count,
            consumedCandidates.Count,
            candidateRefs.Select(FormatCandidateRef).ToArray());
    }

    private static string FormatCandidateRef(TunAttributionCandidate candidate)
    {
        return $"{candidate.Application}({candidate.ProcessId}) {candidate.LocalAddress}:{candidate.LocalPort} -> {candidate.RemoteAddress}:{candidate.RemotePort}";
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

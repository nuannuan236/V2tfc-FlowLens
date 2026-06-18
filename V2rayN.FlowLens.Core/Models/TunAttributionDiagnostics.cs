namespace V2rayN.FlowLens.Core.Models;

public sealed record TunAttributionDiagnostics(
    DateTime RefreshTime,
    int MatchWindowSeconds,
    IReadOnlyList<TunCandidateDiagnostic> Candidates,
    IReadOnlyList<TunRouteEvidenceDiagnostic> RouteEvidence,
    IReadOnlyList<TunMatchDecision> Decisions,
    int MatchedCount,
    int ProbableCount,
    int AmbiguousCount,
    int UnknownCount);

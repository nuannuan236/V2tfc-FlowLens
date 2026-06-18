namespace V2rayN.FlowLens.Core.Models;

public sealed record TunMatchDecision(
    string Target,
    string Outbound,
    AttributionConfidence Confidence,
    string Result,
    string Reason,
    int ExactCandidateCount,
    int AvailableExactCandidateCount,
    int ProbableCandidateCount,
    int ConsumedCandidateCount,
    IReadOnlyList<string> CandidateRefs);

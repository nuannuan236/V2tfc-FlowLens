using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class TunAttributionEngineTests
{
    [Fact]
    public void Attribute_ReturnsMatchedForUniqueIpAndPortCandidate()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("chrome.exe", row.Application);
        Assert.Equal("proxy", row.Outbound);
        Assert.Equal("Matched", row.Status);
        Assert.Equal(AttributionMode.Tun, row.AttributionMode);
        Assert.Equal(AttributionConfidence.Matched, row.Confidence);
    }

    [Fact]
    public void AttributeWithDiagnostics_RecordsMatchedExactCandidateCounts()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().AttributeWithDiagnostics(snapshots, logs, Settings(), Traffic(), now);

        var decision = Assert.Single(result.Diagnostics.Decisions);
        Assert.Equal(AttributionConfidence.Matched, decision.Confidence);
        Assert.Equal("Matched", decision.Result);
        Assert.Equal(1, decision.ExactCandidateCount);
        Assert.Equal(1, decision.AvailableExactCandidateCount);
        Assert.Equal(0, decision.ConsumedCandidateCount);
        Assert.Contains("chrome.exe(100)", Assert.Single(decision.CandidateRefs));
    }

    [Fact]
    public void Attribute_ReturnsProbableForUniqueDomainPortCandidate()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "msedge.exe", 101, "20.205.243.166", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "github.com", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("msedge.exe", row.Application);
        Assert.Equal("Probable", row.Status);
        Assert.Equal(AttributionConfidence.Probable, row.Confidence);
    }

    [Fact]
    public void AttributeWithDiagnostics_RecordsProbableDomainCandidate()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "msedge.exe", 101, "20.205.243.166", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "github.com", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().AttributeWithDiagnostics(snapshots, logs, Settings(), Traffic(), now);

        var decision = Assert.Single(result.Diagnostics.Decisions);
        Assert.Equal(AttributionConfidence.Probable, decision.Confidence);
        Assert.Equal("Probable", decision.Result);
        Assert.Equal(0, decision.ExactCandidateCount);
        Assert.Equal(1, decision.ProbableCandidateCount);
        Assert.Contains("msedge.exe(101)", Assert.Single(decision.CandidateRefs));
    }

    [Fact]
    public void Attribute_ReturnsAmbiguousForMultipleCandidates()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "20.205.243.166", 443),
            CreateSnapshot(now, "msedge.exe", 101, "20.205.243.166", 443, localPort: 50001)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "20.205.243.166", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("Unknown", row.Application);
        Assert.Null(row.ProcessId);
        Assert.Equal("Ambiguous", row.Status);
        Assert.Equal(AttributionConfidence.Ambiguous, row.Confidence);
        Assert.Contains("chrome.exe", row.Evidence);
        Assert.Contains("msedge.exe", row.Evidence);
    }

    [Fact]
    public void AttributeWithDiagnostics_MarksPidZeroIdleCandidateInAmbiguousDecision()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "curl.exe", 200, "142.250.72.14", 443),
            CreateSnapshot(now, "Idle.exe", 0, "142.250.72.14", 443, localPort: 50001)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().AttributeWithDiagnostics(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result.Connections);
        Assert.Equal(AttributionConfidence.Ambiguous, row.Confidence);
        var idleCandidate = Assert.Single(result.Diagnostics.Candidates, candidate => candidate.ProcessId == 0);
        Assert.Equal("Idle.exe", idleCandidate.Application);
        Assert.True(idleCandidate.IsPidZeroOrIdle);
        var decision = Assert.Single(result.Diagnostics.Decisions);
        Assert.Equal("Ambiguous", decision.Result);
        Assert.Equal(2, decision.AvailableExactCandidateCount);
        Assert.Contains(decision.CandidateRefs, candidateRef => candidateRef.StartsWith("Idle.exe(0)", StringComparison.Ordinal));
    }

    [Fact]
    public void Attribute_ReturnsUnknownWhenRouteEvidenceHasNoCandidate()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw")
        };

        var result = new TunAttributionEngine().Attribute([], logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("Unknown", row.Status);
        Assert.Equal(AttributionConfidence.Unknown, row.Confidence);
    }

    [Fact]
    public void Attribute_ReturnsUnknownTcpCandidateWhenLogsAreMissing()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };

        var result = new TunAttributionEngine().Attribute(snapshots, [], Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("chrome.exe", row.Application);
        Assert.Equal("unknown", row.Outbound);
        Assert.Equal("Unknown", row.Status);
        Assert.Equal(AttributionConfidence.Unknown, row.Confidence);
    }

    [Fact]
    public void AttributeWithDiagnostics_ExportsCandidatesWhenLogsAreMissing()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };

        var result = new TunAttributionEngine().AttributeWithDiagnostics(snapshots, [], Settings(), Traffic(), now);

        Assert.Empty(result.Diagnostics.RouteEvidence);
        Assert.Single(result.Diagnostics.Candidates);
        var decision = Assert.Single(result.Diagnostics.Decisions);
        Assert.Equal("NoRouteEvidence", decision.Result);
        Assert.Contains("No route log evidence", decision.Reason);
    }

    [Fact]
    public void Attribute_AppliesProxyOnlyFilterWhenLogsAreMissing()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };
        var settings = Settings() with { OnlyShowProxy = true };

        var result = new TunAttributionEngine().Attribute(snapshots, [], settings, Traffic(), now);

        Assert.Empty(result);
    }

    [Fact]
    public void AttributeWithDiagnostics_KeepsRawDiagnosticsWhenProxyOnlyFiltersRows()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "msedge.exe", 101, "110.242.68.66", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "110.242.68.66", 443, "tun", "direct", "raw direct")
        };
        var settings = Settings() with { OnlyShowProxy = true };

        var result = new TunAttributionEngine().AttributeWithDiagnostics(snapshots, logs, settings, Traffic(), now);

        Assert.Empty(result.Connections);
        Assert.Single(result.Diagnostics.Candidates);
        Assert.Single(result.Diagnostics.RouteEvidence);
        var decision = Assert.Single(result.Diagnostics.Decisions);
        Assert.Equal("direct", decision.Outbound);
        Assert.Equal(AttributionConfidence.Matched, decision.Confidence);
    }

    [Fact]
    public void Attribute_DoesNotDuplicateExactCandidateForRepeatedRouteEvidence()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw 1"),
            new LogConnectionRecord(now.AddMilliseconds(-200), "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw 2")
        };
        var traffic = new Dictionary<TrafficFlowKey, TrafficCounters>
        {
            [new TrafficFlowKey(100, "192.168.1.10", 50000, "142.250.72.14", 443)] = new(1000, 2000)
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), traffic, now);

        var row = Assert.Single(result);
        Assert.Equal("Matched", row.Status);
        Assert.Equal(3000, row.TotalBytes);
        var summary = Assert.Single(new AttributionEngine().SummarizeApplications(result));
        Assert.Equal(3000, summary.TotalBytes);
    }

    [Fact]
    public void Attribute_DoesNotDuplicateProbableCandidateForRepeatedDomainEvidence()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "msedge.exe", 101, "20.205.243.166", 443)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "github.com", 443, "tun", "proxy", "raw 1"),
            new LogConnectionRecord(now.AddMilliseconds(-200), "0.0.0.0", 0, "github.com", 443, "tun", "proxy", "raw 2")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("Probable", row.Status);
    }

    [Fact]
    public void Attribute_AppliesProxyOnlyFilterWhenEnabled()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443),
            CreateSnapshot(now, "msedge.exe", 101, "110.242.68.66", 443, localPort: 50001),
            CreateSnapshot(now, "telegram.exe", 102, "91.108.56.1", 443, localPort: 50002)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw proxy"),
            new LogConnectionRecord(now, "0.0.0.0", 0, "110.242.68.66", 443, "tun", "direct", "raw direct")
        };
        var settings = Settings() with { OnlyShowProxy = true };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, settings, Traffic(), now);

        var row = Assert.Single(result);
        Assert.Equal("proxy", row.Outbound);
        Assert.Equal("chrome.exe", row.Application);
    }

    [Fact]
    public void Attribute_KeepsDirectAndUnknownWhenProxyOnlyDisabled()
    {
        var now = new DateTime(2026, 6, 18, 10, 0, 0);
        var snapshots = new[]
        {
            CreateSnapshot(now, "chrome.exe", 100, "142.250.72.14", 443),
            CreateSnapshot(now, "msedge.exe", 101, "110.242.68.66", 443, localPort: 50001),
            CreateSnapshot(now, "telegram.exe", 102, "91.108.56.1", 443, localPort: 50002)
        };
        var logs = new[]
        {
            new LogConnectionRecord(now, "0.0.0.0", 0, "142.250.72.14", 443, "tun", "proxy", "raw proxy"),
            new LogConnectionRecord(now, "0.0.0.0", 0, "110.242.68.66", 443, "tun", "direct", "raw direct")
        };

        var result = new TunAttributionEngine().Attribute(snapshots, logs, Settings(), Traffic(), now);

        Assert.Contains(result, row => row.Outbound == "proxy");
        Assert.Contains(result, row => row.Outbound == "direct");
        Assert.Contains(result, row => row.Outbound == "unknown");
    }

    private static FlowLensSettings Settings()
    {
        return new FlowLensSettings
        {
            AttributionMode = AttributionMode.Tun,
            HideCoreProcesses = true
        };
    }

    private static Dictionary<TrafficFlowKey, TrafficCounters> Traffic()
    {
        return new Dictionary<TrafficFlowKey, TrafficCounters>();
    }

    private static ConnectionSnapshot CreateSnapshot(
        DateTime now,
        string processName,
        int processId,
        string remoteAddress,
        int remotePort,
        int localPort = 50000)
    {
        return new ConnectionSnapshot(
            new TcpConnectionInfo("192.168.1.10", localPort, remoteAddress, remotePort, processId, processName, "Established"),
            now,
            true);
    }
}

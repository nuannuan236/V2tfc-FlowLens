using System.Text.Json;
using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class TunDiagnosticsJsonExporterTests
{
    [Fact]
    public void Serialize_WritesCamelCaseDiagnosticsJson()
    {
        var diagnostics = new TunAttributionDiagnostics(
            new DateTime(2026, 6, 18, 10, 0, 0),
            5,
            [
                new TunCandidateDiagnostic("curl.exe", 200, "192.168.1.10", 50000, "142.250.72.14", 443, new DateTime(2026, 6, 18, 10, 0, 0), 123, 456, false)
            ],
            [
                new TunRouteEvidenceDiagnostic(new DateTime(2026, 6, 18, 10, 0, 0), "142.250.72.14", 443, "tun", "proxy", "raw")
            ],
            [
                new TunMatchDecision("142.250.72.14:443", "proxy", AttributionConfidence.Matched, "Matched", "reason", 1, 1, 0, 0, ["curl.exe(200) 192.168.1.10:50000 -> 142.250.72.14:443"])
            ],
            1,
            0,
            0,
            0);

        var json = TunDiagnosticsJsonExporter.Serialize(diagnostics);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("refreshTime", out _));
        Assert.Equal(5, root.GetProperty("matchWindowSeconds").GetInt32());
        Assert.Equal("curl.exe", root.GetProperty("candidates")[0].GetProperty("application").GetString());
        Assert.Equal("raw", root.GetProperty("routeEvidence")[0].GetProperty("rawLine").GetString());
        Assert.Equal("Matched", root.GetProperty("decisions")[0].GetProperty("result").GetString());
        Assert.Equal("Matched", root.GetProperty("decisions")[0].GetProperty("confidence").GetString());
    }

    [Fact]
    public void Serialize_ReturnsUnavailableJsonWhenDiagnosticsMissing()
    {
        var json = TunDiagnosticsJsonExporter.Serialize(null);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.False(root.GetProperty("available").GetBoolean());
        Assert.Equal("No TUN diagnostics captured.", root.GetProperty("reason").GetString());
    }
}

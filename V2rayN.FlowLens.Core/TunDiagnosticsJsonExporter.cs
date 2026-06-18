using System.Text.Json;
using System.Text.Json.Serialization;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public static class TunDiagnosticsJsonExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(TunAttributionDiagnostics? diagnostics)
    {
        if (diagnostics is null)
        {
            return SerializeUnavailable("No TUN diagnostics captured.");
        }

        return JsonSerializer.Serialize(diagnostics, JsonOptions);
    }

    public static string SerializeUnavailable(string reason)
    {
        return JsonSerializer.Serialize(new TunDiagnosticsUnavailable(false, reason), JsonOptions);
    }

    private sealed record TunDiagnosticsUnavailable(bool Available, string Reason);
}

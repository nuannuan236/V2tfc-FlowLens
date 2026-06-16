using System.Text;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class SessionCsvExporter
{
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public void WriteApplications(Stream stream, IEnumerable<ApplicationTrafficSummary> rows)
    {
        using var writer = new StreamWriter(stream, Utf8WithBom, leaveOpen: true);
        WriteRow(writer, "Application", "PID", "TotalBytes", "ProxyBytes", "DirectBytes", "UnknownBytes", "ConnectionCount", "LastActive");

        foreach (var row in rows)
        {
            WriteRow(
                writer,
                row.Application,
                row.ProcessId?.ToString() ?? string.Empty,
                row.TotalBytes.ToString(),
                row.ProxyBytes.ToString(),
                row.DirectBytes.ToString(),
                row.UnknownBytes.ToString(),
                row.ConnectionCount.ToString(),
                FormatDateTime(row.LastSeen));
        }
    }

    public void WriteDomains(Stream stream, IEnumerable<DomainTrafficSummary> rows)
    {
        using var writer = new StreamWriter(stream, Utf8WithBom, leaveOpen: true);
        WriteRow(writer, "Domain", "TotalBytes", "ProxyBytes", "DirectBytes", "UnknownBytes", "ConnectionCount", "Applications", "LastActive");

        foreach (var row in rows)
        {
            WriteRow(
                writer,
                row.Domain,
                row.TotalBytes.ToString(),
                row.ProxyBytes.ToString(),
                row.DirectBytes.ToString(),
                row.UnknownBytes.ToString(),
                row.ConnectionCount.ToString(),
                row.Applications,
                FormatDateTime(row.LastSeen));
        }
    }

    private static void WriteRow(TextWriter writer, params string[] values)
    {
        writer.WriteLine(string.Join(",", values.Select(Escape)));
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string FormatDateTime(DateTime value)
    {
        return value.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

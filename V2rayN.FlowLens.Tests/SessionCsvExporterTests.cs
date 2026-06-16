using System.Text;
using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class SessionCsvExporterTests
{
    [Fact]
    public void WriteApplications_WritesHeaderAndDataRows()
    {
        var csv = WriteApplications(
        [
            new ApplicationTrafficSummary(
                "chrome.exe",
                1234,
                2,
                1,
                1,
                0,
                0,
                3000,
                1000,
                2000,
                0,
                new DateTime(2026, 6, 16, 9, 1, 2))
        ]);

        Assert.StartsWith("Application,PID,TotalBytes,ProxyBytes,DirectBytes,UnknownBytes,ConnectionCount,LastActive", csv);
        Assert.Contains("chrome.exe,1234,3000,1000,2000,0,2,2026-06-16 09:01:02", csv);
    }

    [Fact]
    public void WriteDomains_WritesHeaderAndDataRows()
    {
        var csv = WriteDomains(
        [
            new DomainTrafficSummary(
                "github.com",
                3,
                "chrome.exe, msedge.exe",
                2,
                1,
                0,
                4096,
                3000,
                1096,
                0,
                new DateTime(2026, 6, 16, 9, 2, 3))
        ]);

        Assert.StartsWith("Domain,TotalBytes,ProxyBytes,DirectBytes,UnknownBytes,ConnectionCount,Applications,LastActive", csv);
        Assert.Contains("github.com,4096,3000,1096,0,3,\"chrome.exe, msedge.exe\",2026-06-16 09:02:03", csv);
    }

    [Fact]
    public void WriteApplications_EscapesCsvSpecialCharacters()
    {
        var csv = WriteApplications(
        [
            new ApplicationTrafficSummary(
                "weird, \"app\"\nname.exe",
                null,
                1,
                0,
                0,
                0,
                1,
                42,
                0,
                0,
                42,
                new DateTime(2026, 6, 16, 9, 3, 4))
        ]);

        Assert.Contains("\"weird, \"\"app\"\"\nname.exe\",,42,0,0,42,1,2026-06-16 09:03:04", csv);
    }

    [Fact]
    public void WriteDomains_EmptyRowsWritesOnlyHeader()
    {
        var csv = WriteDomains([]);
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        var header = Assert.Single(lines);
        Assert.Equal("Domain,TotalBytes,ProxyBytes,DirectBytes,UnknownBytes,ConnectionCount,Applications,LastActive", header);
    }

    [Fact]
    public void WriteApplications_EmitsUtf8Bom()
    {
        var bytes = WriteApplicationBytes([]);

        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    private static string WriteApplications(IEnumerable<ApplicationTrafficSummary> rows)
    {
        return Encoding.UTF8.GetString(WriteApplicationBytes(rows)[3..]);
    }

    private static byte[] WriteApplicationBytes(IEnumerable<ApplicationTrafficSummary> rows)
    {
        using var stream = new MemoryStream();
        new SessionCsvExporter().WriteApplications(stream, rows);
        return stream.ToArray();
    }

    private static string WriteDomains(IEnumerable<DomainTrafficSummary> rows)
    {
        using var stream = new MemoryStream();
        new SessionCsvExporter().WriteDomains(stream, rows);
        return Encoding.UTF8.GetString(stream.ToArray()[3..]);
    }
}

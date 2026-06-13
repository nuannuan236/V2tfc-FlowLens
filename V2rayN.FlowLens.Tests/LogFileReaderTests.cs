using V2rayN.FlowLens.Core;

namespace V2rayN.FlowLens.Tests;

public sealed class LogFileReaderTests
{
    [Fact]
    public void ReadRecent_ReadsVaccessFileFromGuiLogsWhenRootDirectoryIsSelected()
    {
        using var tempDirectory = new TempDirectory();
        var guiLogsPath = Path.Combine(tempDirectory.Path, "guiLogs");
        Directory.CreateDirectory(guiLogsPath);
        var logPath = Path.Combine(guiLogsPath, "Vaccess_2026-06-13.txt");
        File.WriteAllText(
            logPath,
            "2026/06/13 22:13:14.573031 from 127.0.0.1:4555 accepted //acrobat.adobe.com:443 [socks >> proxy]");

        var reader = new LogFileReader(new LogParser());

        var records = reader.ReadRecent(tempDirectory.Path);

        var record = Assert.Single(records);
        Assert.Equal(4555, record.SourcePort);
        Assert.Equal("acrobat.adobe.com", record.TargetHost);
        Assert.Equal("proxy", record.Outbound);
    }

    [Fact]
    public void ReadRecentWithInfo_ReturnsResolvedLogFiles()
    {
        using var tempDirectory = new TempDirectory();
        var guiLogsPath = Path.Combine(tempDirectory.Path, "guiLogs");
        Directory.CreateDirectory(guiLogsPath);
        var logPath = Path.Combine(guiLogsPath, "Vaccess_2026-06-13.txt");
        File.WriteAllText(
            logPath,
            "2026/06/13 22:13:14.573031 from 127.0.0.1:4555 accepted //acrobat.adobe.com:443 [socks >> proxy]");

        var reader = new LogFileReader(new LogParser());

        var result = reader.ReadRecentWithInfo(tempDirectory.Path);

        Assert.Single(result.Records);
        Assert.Contains(logPath, result.Files);
    }

    [Fact]
    public void ReadRecent_ReadsLogFileWhileAnotherProcessKeepsItOpenForWriting()
    {
        using var tempDirectory = new TempDirectory();
        var logPath = Path.Combine(tempDirectory.Path, "Vaccess_2026-06-13.txt");
        File.WriteAllText(
            logPath,
            "2026/06/13 22:13:14.573031 from 127.0.0.1:4555 accepted //acrobat.adobe.com:443 [socks >> proxy]");

        using var writerHandle = new FileStream(logPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        var reader = new LogFileReader(new LogParser());

        var records = reader.ReadRecent(tempDirectory.Path);

        Assert.Single(records);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"flowlens-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}

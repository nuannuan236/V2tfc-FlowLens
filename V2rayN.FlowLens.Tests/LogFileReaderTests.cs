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

    [Fact]
    public void ReadRecent_AutoDiscoversV2rayNRootWhenPathIsEmpty()
    {
        using var tempDirectory = new TempDirectory();
        var v2rayNRoot = Path.Combine(tempDirectory.Path, "tools", "v2rayN-windows-64-SelfContained");
        var guiLogsPath = Path.Combine(v2rayNRoot, "guiLogs");
        Directory.CreateDirectory(guiLogsPath);
        File.WriteAllText(Path.Combine(v2rayNRoot, "v2rayN.exe"), string.Empty);
        var logPath = Path.Combine(guiLogsPath, "Vaccess_2026-06-13.txt");
        File.WriteAllText(
            logPath,
            "2026/06/13 22:13:14.573031 from 127.0.0.1:4555 accepted //acrobat.adobe.com:443 [socks >> proxy]");
        using var discoveryRoots = new ScopedEnvironmentVariable("V2RAYN_FLOWLENS_DISCOVERY_ROOTS", tempDirectory.Path);
        var reader = new LogFileReader(new LogParser());

        var result = reader.ReadRecentWithInfo(string.Empty);

        var record = Assert.Single(result.Records);
        Assert.Equal(4555, record.SourcePort);
        Assert.Contains(logPath, result.Files);
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

    private sealed class ScopedEnvironmentVariable : IDisposable
    {
        private readonly string name;
        private readonly string? previousValue;

        public ScopedEnvironmentVariable(string name, string value)
        {
            this.name = name;
            previousValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(name, previousValue);
        }
    }
}

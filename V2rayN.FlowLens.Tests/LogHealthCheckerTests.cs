using V2rayN.FlowLens.Core;

namespace V2rayN.FlowLens.Tests;

public sealed class LogHealthCheckerTests
{
    [Fact]
    public void Check_ReturnsCoreRoutingLogOk_WhenRecentLogContainsAcceptedRoute()
    {
        using var tempDirectory = new TempDirectory();
        var logPath = Path.Combine(tempDirectory.Path, "Vaccess_2026-06-13.txt");
        File.WriteAllText(
            logPath,
            "2026/06/13 16:39:10 from 127.0.0.1:9852 accepted //www.google-analytics.com:443 [socks -> proxy]");

        var checker = new LogHealthChecker(new LogParser());

        var status = checker.Check(tempDirectory.Path);

        Assert.Equal(LogHealthStatus.CoreRoutingLogOk, status);
    }

    [Fact]
    public void Check_ReturnsNoCoreRoutingLog_WhenLogsDoNotContainAcceptedRoute()
    {
        using var tempDirectory = new TempDirectory();
        var logPath = Path.Combine(tempDirectory.Path, "2026-06-13.txt");
        File.WriteAllText(logPath, "2026-06-13 15:02:51.3817-DEBUG GUI log line");

        var checker = new LogHealthChecker(new LogParser());

        var status = checker.Check(tempDirectory.Path);

        Assert.Equal(LogHealthStatus.NoCoreRoutingLog, status);
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

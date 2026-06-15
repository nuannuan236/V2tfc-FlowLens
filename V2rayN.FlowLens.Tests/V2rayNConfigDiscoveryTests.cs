using V2rayN.FlowLens.Core;

namespace V2rayN.FlowLens.Tests;

public sealed class V2rayNConfigDiscoveryTests
{
    [Fact]
    public void Discover_ReadsInboundLocalPortFromRootDirectory()
    {
        using var tempDirectory = new TempDirectory();
        var root = CreateV2rayNRoot(tempDirectory.Path, 10808);
        var discovery = new V2rayNConfigDiscovery();

        var result = discovery.Discover(root, []);

        Assert.Equal(V2rayNConfigDiscoveryStatus.Found, result.Status);
        Assert.Equal(root, result.RootDirectory);
        Assert.Equal([10808], result.CandidatePorts.Order());
    }

    [Fact]
    public void Discover_NormalizesGuiLogsPathToRootDirectory()
    {
        using var tempDirectory = new TempDirectory();
        var root = CreateV2rayNRoot(tempDirectory.Path, 10810);
        var discovery = new V2rayNConfigDiscovery();

        var result = discovery.Discover(Path.Combine(root, "guiLogs"), []);

        Assert.Equal(V2rayNConfigDiscoveryStatus.Found, result.Status);
        Assert.Equal(root, result.RootDirectory);
        Assert.Equal([10810], result.CandidatePorts.Order());
    }

    [Fact]
    public void Discover_ReadsInboundLocalPortWhenInboundIsArray()
    {
        using var tempDirectory = new TempDirectory();
        var root = CreateV2rayNRoot(tempDirectory.Path, 10808);
        File.WriteAllText(
            Path.Combine(root, "guiConfigs", "guiNConfig.json"),
            """
            {
              "Inbound": [
                {
                  "LocalPort": 10808,
                  "Protocol": "socks"
                }
              ]
            }
            """);
        var discovery = new V2rayNConfigDiscovery();

        var result = discovery.Discover(root, []);

        Assert.Equal(V2rayNConfigDiscoveryStatus.Found, result.Status);
        Assert.Equal([10808], result.CandidatePorts.Order());
    }

    [Fact]
    public void Discover_NormalizesGuiConfigsPathToRootDirectory()
    {
        using var tempDirectory = new TempDirectory();
        var root = CreateV2rayNRoot(tempDirectory.Path, 10812);
        var discovery = new V2rayNConfigDiscovery();

        var result = discovery.Discover(Path.Combine(root, "guiConfigs"), []);

        Assert.Equal(V2rayNConfigDiscoveryStatus.Found, result.Status);
        Assert.Equal(root, result.RootDirectory);
        Assert.Equal([10812], result.CandidatePorts.Order());
    }

    [Fact]
    public void Discover_UsesResolvedLogFileToFindRootDirectory()
    {
        using var tempDirectory = new TempDirectory();
        var root = CreateV2rayNRoot(tempDirectory.Path, 10808);
        var logPath = Path.Combine(root, "guiLogs", "Vaccess_2026-06-14.txt");
        File.WriteAllText(logPath, string.Empty);
        var discovery = new V2rayNConfigDiscovery();

        var result = discovery.Discover(string.Empty, [logPath]);

        Assert.Equal(V2rayNConfigDiscoveryStatus.Found, result.Status);
        Assert.Equal(root, result.RootDirectory);
        Assert.Equal([10808], result.CandidatePorts.Order());
    }

    [Fact]
    public void Discover_ReturnsParseFailedWhenConfigIsInvalid()
    {
        using var tempDirectory = new TempDirectory();
        var root = CreateV2rayNRoot(tempDirectory.Path, 10808);
        File.WriteAllText(Path.Combine(root, "guiConfigs", "guiNConfig.json"), "{ invalid");
        var discovery = new V2rayNConfigDiscovery();

        var result = discovery.Discover(root, []);

        Assert.Equal(V2rayNConfigDiscoveryStatus.ParseFailed, result.Status);
        Assert.Empty(result.CandidatePorts);
    }

    private static string CreateV2rayNRoot(string parent, int port)
    {
        var root = Path.Combine(parent, "v2rayN-windows-64-SelfContained");
        Directory.CreateDirectory(Path.Combine(root, "guiLogs"));
        Directory.CreateDirectory(Path.Combine(root, "guiConfigs"));
        File.WriteAllText(Path.Combine(root, "v2rayN.exe"), string.Empty);
        File.WriteAllText(
            Path.Combine(root, "guiConfigs", "guiNConfig.json"),
            $$"""
            {
              "Inbound": {
                "LocalPort": {{port}},
                "Protocol": "socks"
              }
            }
            """);

        return root;
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

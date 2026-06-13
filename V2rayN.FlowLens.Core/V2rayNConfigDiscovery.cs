using System.Text.Json;

namespace V2rayN.FlowLens.Core;

public enum V2rayNConfigDiscoveryStatus
{
    Found,
    NotFound,
    ParseFailed
}

public sealed record V2rayNConfigDiscoveryResult(
    V2rayNConfigDiscoveryStatus Status,
    string RootDirectory,
    IReadOnlySet<int> CandidatePorts,
    string Message);

public sealed class V2rayNConfigDiscovery
{
    private const string DiscoveryRootsEnvironmentVariable = "V2RAYN_FLOWLENS_DISCOVERY_ROOTS";

    public V2rayNConfigDiscoveryResult Discover(string inputPath, IEnumerable<string> resolvedLogFiles)
    {
        foreach (var root in GetCandidateRoots(inputPath, resolvedLogFiles).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var result = TryReadRoot(root);
            if (result.Status != V2rayNConfigDiscoveryStatus.NotFound)
            {
                return result;
            }
        }

        return new V2rayNConfigDiscoveryResult(
            V2rayNConfigDiscoveryStatus.NotFound,
            string.Empty,
            new HashSet<int>(),
            "v2rayN config was not found.");
    }

    private static IEnumerable<string> GetCandidateRoots(string inputPath, IEnumerable<string> resolvedLogFiles)
    {
        foreach (var root in RootsFromInputPath(inputPath))
        {
            yield return root;
        }

        foreach (var file in resolvedLogFiles)
        {
            foreach (var root in RootsFromInputPath(file))
            {
                yield return root;
            }
        }

        if (!string.IsNullOrWhiteSpace(inputPath) || resolvedLogFiles.Any())
        {
            yield break;
        }

        foreach (var root in AutoDiscoveryRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            if (LooksLikeV2rayNRoot(root))
            {
                yield return root;
            }

            foreach (var directory in EnumerateDirectories(root, maxDepth: 4))
            {
                if (LooksLikeV2rayNRoot(directory))
                {
                    yield return directory;
                }
            }
        }
    }

    private static IEnumerable<string> RootsFromInputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            yield break;
        }

        var current = File.Exists(path)
            ? Directory.GetParent(path)
            : Directory.Exists(path)
                ? new DirectoryInfo(path)
                : null;

        while (current is not null)
        {
            if (IsExplicitRootCandidate(current.FullName))
            {
                yield return NormalizeRoot(current.FullName);
            }

            current = current.Parent;
        }
    }

    private static string NormalizeRoot(string directory)
    {
        var name = Path.GetFileName(directory);
        if (name.Equals("guiLogs", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("guiConfigs", StringComparison.OrdinalIgnoreCase))
        {
            return Directory.GetParent(directory)?.FullName ?? directory;
        }

        return directory;
    }

    private static bool IsExplicitRootCandidate(string directory)
    {
        var normalizedRoot = NormalizeRoot(directory);
        return Directory.Exists(Path.Combine(normalizedRoot, "guiConfigs"))
            || Directory.Exists(Path.Combine(normalizedRoot, "guiLogs"))
            || File.Exists(Path.Combine(normalizedRoot, "v2rayN.exe"));
    }

    private static V2rayNConfigDiscoveryResult TryReadRoot(string root)
    {
        if (!Directory.Exists(root))
        {
            return NotFound();
        }

        var configPath = Path.Combine(root, "guiConfigs", "guiNConfig.json");
        if (!File.Exists(configPath))
        {
            return NotFound();
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath));
            var ports = ReadPorts(document.RootElement);
            if (ports.Count == 0)
            {
                return new V2rayNConfigDiscoveryResult(
                    V2rayNConfigDiscoveryStatus.ParseFailed,
                    root,
                    ports,
                    $"No local proxy port was found in {configPath}.");
            }

            return new V2rayNConfigDiscoveryResult(
                V2rayNConfigDiscoveryStatus.Found,
                root,
                ports,
                $"Read local proxy ports from {configPath}.");
        }
        catch (JsonException ex)
        {
            return ParseFailed(root, ex.Message);
        }
        catch (IOException ex)
        {
            return ParseFailed(root, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ParseFailed(root, ex.Message);
        }

        static V2rayNConfigDiscoveryResult NotFound()
        {
            return new V2rayNConfigDiscoveryResult(
                V2rayNConfigDiscoveryStatus.NotFound,
                string.Empty,
                new HashSet<int>(),
                string.Empty);
        }

        static V2rayNConfigDiscoveryResult ParseFailed(string root, string message)
        {
            return new V2rayNConfigDiscoveryResult(
                V2rayNConfigDiscoveryStatus.ParseFailed,
                root,
                new HashSet<int>(),
                message);
        }
    }

    private static HashSet<int> ReadPorts(JsonElement root)
    {
        var ports = new HashSet<int>();
        if (root.TryGetProperty("Inbound", out var inbound) &&
            inbound.TryGetProperty("LocalPort", out var localPort) &&
            localPort.TryGetInt32(out var port) &&
            port is > 0 and <= 65535)
        {
            ports.Add(port);
        }

        return ports;
    }

    private static IEnumerable<string> AutoDiscoveryRoots()
    {
        var overrideRoots = Environment.GetEnvironmentVariable(DiscoveryRootsEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideRoots))
        {
            foreach (var root in overrideRoots.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return root;
            }

            yield break;
        }

        foreach (var root in GetAncestors(AppContext.BaseDirectory))
        {
            yield return root;
        }

        foreach (var root in GetAncestors(Environment.CurrentDirectory))
        {
            yield return root;
        }

        foreach (var specialFolder in new[]
        {
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolder.ProgramFiles,
            Environment.SpecialFolder.ProgramFilesX86
        })
        {
            var path = Environment.GetFolderPath(specialFolder);
            if (!string.IsNullOrWhiteSpace(path))
            {
                yield return path;
            }
        }

        foreach (var drive in DriveInfo.GetDrives().Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady))
        {
            yield return drive.RootDirectory.FullName;
        }
    }

    private static bool LooksLikeV2rayNRoot(string directory)
    {
        return File.Exists(Path.Combine(directory, "v2rayN.exe")) &&
            Directory.Exists(Path.Combine(directory, "guiConfigs"));
    }

    private static IEnumerable<string> EnumerateDirectories(string root, int maxDepth)
    {
        var pending = new Queue<(string Directory, int Depth)>();
        pending.Enqueue((root, 0));

        while (pending.Count > 0)
        {
            var (current, depth) = pending.Dequeue();
            if (depth >= maxDepth)
            {
                continue;
            }

            foreach (var child in SafeEnumerateDirectories(current))
            {
                if (ShouldSkipDirectory(child))
                {
                    continue;
                }

                yield return child;
                pending.Enqueue((child, depth + 1));
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string directory)
    {
        try
        {
            return Directory.EnumerateDirectories(directory);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool ShouldSkipDirectory(string directory)
    {
        var name = Path.GetFileName(directory);
        return name.Equals("$Recycle.Bin", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("System Volume Information", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Windows", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetAncestors(string path)
    {
        var directory = new DirectoryInfo(path);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }
}

using System.Collections.Concurrent;

namespace V2rayN.FlowLens.Core;

internal static class LogFileResolver
{
    private static readonly string[] AccessLogPatterns = ["Vaccess_*.txt", "Vaccess_*.log"];
    private static readonly string[] FallbackPatterns = ["*.log", "*.txt"];
    private static readonly TimeSpan AutoDiscoveryCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly ConcurrentDictionary<string, CacheEntry> AutoDiscoveryCache = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> ResolveLogFiles(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            return ResolveExplicitLogFiles(path);
        }

        return ResolveAutoDiscoveredLogFiles();
    }

    private static IReadOnlyList<string> ResolveExplicitLogFiles(string path)
    {
        if (File.Exists(path))
        {
            return [path];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        return ResolveLogFilesFromDirectories(ResolveLogDirectories(path));
    }

    private static IReadOnlyList<string> ResolveAutoDiscoveredLogFiles()
    {
        var roots = GetAutoDiscoveryRoots().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var cacheKey = string.Join("|", roots);
        var now = DateTimeOffset.UtcNow;

        if (AutoDiscoveryCache.TryGetValue(cacheKey, out var cached) && now - cached.CreatedAt < AutoDiscoveryCacheDuration)
        {
            return cached.Files;
        }

        var files = Array.Empty<string>();
        foreach (var root in roots)
        {
            files = FindV2rayNLogDirectories(root)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(directory => ResolveLogFilesFromDirectories([directory]))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(5)
                .ToArray();

            if (files.Length > 0)
            {
                break;
            }
        }

        AutoDiscoveryCache[cacheKey] = new CacheEntry(now, files);
        return files;
    }

    private static IReadOnlyList<string> ResolveLogFilesFromDirectories(IEnumerable<string> directories)
    {
        var existingDirectories = directories.Where(Directory.Exists).ToArray();
        var accessLogs = ResolveLogFilesFromDirectories(existingDirectories, AccessLogPatterns);

        return accessLogs.Count > 0
            ? accessLogs
            : ResolveLogFilesFromDirectories(existingDirectories, FallbackPatterns);
    }

    private static IReadOnlyList<string> ResolveLogFilesFromDirectories(IEnumerable<string> directories, string[] patterns)
    {
        return directories
            .Where(Directory.Exists)
            .SelectMany(directory => patterns.SelectMany(pattern => Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(5)
            .ToArray();
    }

    private static IReadOnlyList<string> ResolveLogDirectories(string path)
    {
        var directories = new List<string> { path };
        var guiLogsPath = Path.Combine(path, "guiLogs");
        if (Directory.Exists(guiLogsPath))
        {
            directories.Add(guiLogsPath);
        }

        return directories;
    }

    private static IEnumerable<string> FindV2rayNLogDirectories(string root)
    {
        if (!Directory.Exists(root))
        {
            yield break;
        }

        if (LooksLikeV2rayNRoot(root))
        {
            yield return Path.Combine(root, "guiLogs");
        }

        foreach (var directory in EnumerateDirectories(root, maxDepth: 4))
        {
            if (LooksLikeV2rayNRoot(directory))
            {
                yield return Path.Combine(directory, "guiLogs");
            }
        }
    }

    private static bool LooksLikeV2rayNRoot(string directory)
    {
        return File.Exists(Path.Combine(directory, "v2rayN.exe")) &&
            Directory.Exists(Path.Combine(directory, "guiLogs"));
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

    private static IEnumerable<string> GetAutoDiscoveryRoots()
    {
        var overrideRoots = Environment.GetEnvironmentVariable("V2RAYN_FLOWLENS_DISCOVERY_ROOTS");
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

    private static IEnumerable<string> GetAncestors(string path)
    {
        var directory = new DirectoryInfo(path);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }

    private sealed record CacheEntry(DateTimeOffset CreatedAt, IReadOnlyList<string> Files);
}

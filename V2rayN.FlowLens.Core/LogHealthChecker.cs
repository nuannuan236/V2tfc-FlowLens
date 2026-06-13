namespace V2rayN.FlowLens.Core;

public enum LogHealthStatus
{
    NoCoreRoutingLog,
    CoreRoutingLogOk
}

public sealed class LogHealthChecker(LogParser parser)
{
    private static readonly string[] SupportedPatterns = ["*.log", "*.txt"];

    public LogHealthStatus Check(string path, int maxLinesPerFile = 5000)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LogHealthStatus.NoCoreRoutingLog;
        }

        foreach (var file in ResolveLogFiles(path))
        {
            foreach (var line in ReadTailLines(file, maxLinesPerFile))
            {
                if (parser.TryParse(line, out _))
                {
                    return LogHealthStatus.CoreRoutingLogOk;
                }
            }
        }

        return LogHealthStatus.NoCoreRoutingLog;
    }

    private static IReadOnlyList<string> ResolveLogFiles(string path)
    {
        if (File.Exists(path))
        {
            return [path];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        var directories = ResolveLogDirectories(path);

        return directories
            .SelectMany(directory => SupportedPatterns.SelectMany(pattern => Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly)))
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

    private static IEnumerable<string> ReadTailLines(string file, int maxLines)
    {
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            var lines = new List<string>();

            while (reader.ReadLine() is { } line)
            {
                lines.Add(line);
                if (lines.Count > maxLines)
                {
                    lines.RemoveAt(0);
                }
            }

            return lines;
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
}

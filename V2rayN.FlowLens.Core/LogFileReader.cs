using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class LogFileReader(LogParser parser)
{
    private static readonly string[] SupportedPatterns = ["*.log", "*.txt"];

    public IReadOnlyList<LogConnectionRecord> ReadRecent(string path, int maxLinesPerFile = 5000)
    {
        return ReadRecentWithInfo(path, maxLinesPerFile).Records;
    }

    public LogReadResult ReadRecentWithInfo(string path, int maxLinesPerFile = 5000)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new LogReadResult([], []);
        }

        var files = ResolveLogFiles(path);
        var records = new List<LogConnectionRecord>();

        foreach (var file in files)
        {
            foreach (var line in ReadTailLines(file, maxLinesPerFile))
            {
                if (parser.TryParse(line, out var record) && record is not null)
                {
                    records.Add(record);
                }
            }
        }

        var orderedRecords = records
            .OrderByDescending(record => record.Timestamp)
            .Take(maxLinesPerFile)
            .ToArray();

        return new LogReadResult(orderedRecords, files);
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

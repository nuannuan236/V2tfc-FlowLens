using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class LogFileReader(LogParser parser)
{
    public IReadOnlyList<LogConnectionRecord> ReadRecent(string path, int maxLinesPerFile = 5000)
    {
        return ReadRecentWithInfo(path, maxLinesPerFile).Records;
    }

    public LogReadResult ReadRecentWithInfo(string path, int maxLinesPerFile = 5000)
    {
        var files = LogFileResolver.ResolveLogFiles(path);
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

namespace V2rayN.FlowLens.Core;

public enum LogHealthStatus
{
    NoCoreRoutingLog,
    CoreRoutingLogOk
}

public sealed class LogHealthChecker(LogParser parser)
{
    public LogHealthStatus Check(string path, int maxLinesPerFile = 5000)
    {
        foreach (var file in LogFileResolver.ResolveLogFiles(path))
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

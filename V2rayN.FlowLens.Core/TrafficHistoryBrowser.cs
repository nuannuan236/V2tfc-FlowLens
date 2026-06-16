using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class TrafficHistoryBrowser
{
    private readonly TodayTrafficHistoryStore store;

    public TrafficHistoryBrowser()
        : this(new TodayTrafficHistoryStore())
    {
    }

    public TrafficHistoryBrowser(TodayTrafficHistoryStore store)
    {
        this.store = store;
    }

    public string HistoryDirectory => store.HistoryDirectory;

    public IReadOnlyList<TrafficHistoryDateOption> ListDates()
    {
        try
        {
            if (!Directory.Exists(store.HistoryDirectory))
            {
                return [];
            }

            return Directory
                .EnumerateFiles(store.HistoryDirectory, "*.json", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileNameWithoutExtension)
                .Select(fileName => DateOnly.TryParseExact(fileName, "yyyy-MM-dd", out var date)
                    ? new TrafficHistoryDateOption(date, store.GetPath(date))
                    : null)
                .Where(option => option is not null)
                .Cast<TrafficHistoryDateOption>()
                .OrderByDescending(option => option.Date)
                .ToArray();
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

    public TrafficHistoryBrowserResult Load(DateOnly date)
    {
        var result = store.Load(date);
        return new TrafficHistoryBrowserResult(result.IsSuccess, result.History, result.State);
    }
}

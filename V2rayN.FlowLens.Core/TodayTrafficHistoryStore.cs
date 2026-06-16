using System.Text.Json;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class TodayTrafficHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string historyDirectory;

    public TodayTrafficHistoryStore()
        : this(GetDefaultHistoryDirectory())
    {
    }

    public TodayTrafficHistoryStore(string historyDirectory)
    {
        this.historyDirectory = historyDirectory;
    }

    public string GetPath(DateOnly date)
    {
        return Path.Combine(historyDirectory, $"{date:yyyy-MM-dd}.json");
    }

    public string HistoryDirectory => historyDirectory;

    public TodayTrafficHistoryLoadResult Load(DateOnly date)
    {
        var path = GetPath(date);
        try
        {
            if (!File.Exists(path))
            {
                return TodayTrafficHistoryLoadResult.Success(TodayTrafficHistory.Empty(date), path, "No history file yet");
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return TodayTrafficHistoryLoadResult.Failure(TodayTrafficHistory.Empty(date), path, "Today history unavailable: file is empty");
            }

            var dto = JsonSerializer.Deserialize<TodayTrafficHistoryDto>(json, JsonOptions);
            if (dto is null)
            {
                return TodayTrafficHistoryLoadResult.Failure(TodayTrafficHistory.Empty(date), path, "Today history unavailable: file could not be read");
            }

            var history = new TodayTrafficHistory(
                date,
                dto.Applications.Select(ToApplicationSummary).ToArray(),
                dto.Domains.Select(ToDomainSummary).ToArray());

            return TodayTrafficHistoryLoadResult.Success(history, path, "Loaded");
        }
        catch (JsonException ex)
        {
            return TodayTrafficHistoryLoadResult.Failure(TodayTrafficHistory.Empty(date), path, $"Today history unavailable: invalid JSON ({ex.Message})");
        }
        catch (IOException ex)
        {
            return TodayTrafficHistoryLoadResult.Failure(TodayTrafficHistory.Empty(date), path, $"Today history unavailable: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return TodayTrafficHistoryLoadResult.Failure(TodayTrafficHistory.Empty(date), path, $"Today history unavailable: {ex.Message}");
        }
    }

    public TodayHistoryState Save(TodayTrafficHistory history)
    {
        var path = GetPath(history.Date);
        try
        {
            Directory.CreateDirectory(historyDirectory);
            var dto = new TodayTrafficHistoryDto
            {
                Date = history.Date.ToString("yyyy-MM-dd"),
                Applications = history.Applications.Select(ToApplicationDto).ToArray(),
                Domains = history.Domains.Select(ToDomainDto).ToArray()
            };

            File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
            return new TodayHistoryState(history.Date, path, "Saved");
        }
        catch (IOException ex)
        {
            return new TodayHistoryState(history.Date, path, $"Today history unavailable: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return new TodayHistoryState(history.Date, path, $"Today history unavailable: {ex.Message}");
        }
    }

    public static string GetDefaultHistoryDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "V2rayN.FlowLens", "history");
    }

    private static ApplicationTrafficSummary ToApplicationSummary(ApplicationTrafficSummaryDto dto)
    {
        return new ApplicationTrafficSummary(
            dto.Application ?? string.Empty,
            dto.ProcessId,
            dto.ConnectionCount,
            dto.ProxyCount,
            dto.DirectCount,
            dto.BlockCount,
            dto.UnknownCount,
            dto.TotalBytes,
            dto.ProxyBytes,
            dto.DirectBytes,
            dto.UnknownBytes,
            dto.LastSeen);
    }

    private static DomainTrafficSummary ToDomainSummary(DomainTrafficSummaryDto dto)
    {
        return new DomainTrafficSummary(
            dto.Domain ?? string.Empty,
            dto.ConnectionCount,
            dto.Applications ?? string.Empty,
            dto.ProxyCount,
            dto.DirectCount,
            dto.UnknownCount,
            dto.TotalBytes,
            dto.ProxyBytes,
            dto.DirectBytes,
            dto.UnknownBytes,
            dto.LastSeen);
    }

    private static ApplicationTrafficSummaryDto ToApplicationDto(ApplicationTrafficSummary summary)
    {
        return new ApplicationTrafficSummaryDto
        {
            Application = summary.Application,
            ProcessId = summary.ProcessId,
            ConnectionCount = summary.ConnectionCount,
            ProxyCount = summary.ProxyCount,
            DirectCount = summary.DirectCount,
            BlockCount = summary.BlockCount,
            UnknownCount = summary.UnknownCount,
            TotalBytes = summary.TotalBytes,
            ProxyBytes = summary.ProxyBytes,
            DirectBytes = summary.DirectBytes,
            UnknownBytes = summary.UnknownBytes,
            LastSeen = summary.LastSeen
        };
    }

    private static DomainTrafficSummaryDto ToDomainDto(DomainTrafficSummary summary)
    {
        return new DomainTrafficSummaryDto
        {
            Domain = summary.Domain,
            ConnectionCount = summary.ConnectionCount,
            Applications = summary.Applications,
            ProxyCount = summary.ProxyCount,
            DirectCount = summary.DirectCount,
            UnknownCount = summary.UnknownCount,
            TotalBytes = summary.TotalBytes,
            ProxyBytes = summary.ProxyBytes,
            DirectBytes = summary.DirectBytes,
            UnknownBytes = summary.UnknownBytes,
            LastSeen = summary.LastSeen
        };
    }

    private sealed class TodayTrafficHistoryDto
    {
        public string Date { get; init; } = string.Empty;

        public ApplicationTrafficSummaryDto[] Applications { get; init; } = [];

        public DomainTrafficSummaryDto[] Domains { get; init; } = [];
    }

    private sealed class ApplicationTrafficSummaryDto
    {
        public string? Application { get; init; }

        public int? ProcessId { get; init; }

        public int ConnectionCount { get; init; }

        public int ProxyCount { get; init; }

        public int DirectCount { get; init; }

        public int BlockCount { get; init; }

        public int UnknownCount { get; init; }

        public long TotalBytes { get; init; }

        public long ProxyBytes { get; init; }

        public long DirectBytes { get; init; }

        public long UnknownBytes { get; init; }

        public DateTime LastSeen { get; init; }
    }

    private sealed class DomainTrafficSummaryDto
    {
        public string? Domain { get; init; }

        public int ConnectionCount { get; init; }

        public string? Applications { get; init; }

        public int ProxyCount { get; init; }

        public int DirectCount { get; init; }

        public int UnknownCount { get; init; }

        public long TotalBytes { get; init; }

        public long ProxyBytes { get; init; }

        public long DirectBytes { get; init; }

        public long UnknownBytes { get; init; }

        public DateTime LastSeen { get; init; }
    }
}

public sealed record TodayTrafficHistoryLoadResult(
    bool IsSuccess,
    TodayTrafficHistory History,
    TodayHistoryState State)
{
    public static TodayTrafficHistoryLoadResult Success(TodayTrafficHistory history, string path, string status)
    {
        return new TodayTrafficHistoryLoadResult(true, history, new TodayHistoryState(history.Date, path, status));
    }

    public static TodayTrafficHistoryLoadResult Failure(TodayTrafficHistory history, string path, string status)
    {
        return new TodayTrafficHistoryLoadResult(false, history, new TodayHistoryState(history.Date, path, status));
    }
}

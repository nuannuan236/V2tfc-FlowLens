using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class TodayTrafficHistoryStoreTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "FlowLensTodayTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsTodayHistory()
    {
        var store = new TodayTrafficHistoryStore(tempDirectory);
        var date = new DateOnly(2026, 6, 16);
        var history = new TodayTrafficHistory(
            date,
            [
                new ApplicationTrafficSummary("chrome.exe", 1234, 2, 1, 1, 0, 0, 3000, 1000, 2000, 0, new DateTime(2026, 6, 16, 9, 0, 0))
            ],
            [
                new DomainTrafficSummary("github.com", 2, "chrome.exe", 1, 1, 0, 3000, 1000, 2000, 0, new DateTime(2026, 6, 16, 9, 0, 0))
            ]);

        var saveState = store.Save(history);
        var loadResult = store.Load(date);

        Assert.Equal("Saved", saveState.Status);
        Assert.True(loadResult.IsSuccess);
        Assert.Equal(store.GetPath(date), loadResult.State.FilePath);
        Assert.Equal(3000, Assert.Single(loadResult.History.Applications).TotalBytes);
        Assert.Equal(3000, Assert.Single(loadResult.History.Domains).TotalBytes);
    }

    [Fact]
    public void Load_MissingFileReturnsEmptySuccess()
    {
        var store = new TodayTrafficHistoryStore(tempDirectory);
        var result = store.Load(new DateOnly(2026, 6, 16));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.History.Applications);
        Assert.Contains("No history", result.State.Status);
    }

    [Fact]
    public void Load_EmptyFileReturnsSafeFailure()
    {
        var store = new TodayTrafficHistoryStore(tempDirectory);
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(store.GetPath(new DateOnly(2026, 6, 16)), string.Empty);

        var result = store.Load(new DateOnly(2026, 6, 16));

        Assert.False(result.IsSuccess);
        Assert.Empty(result.History.Applications);
        Assert.Contains("unavailable", result.State.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DamagedJsonReturnsSafeFailure()
    {
        var store = new TodayTrafficHistoryStore(tempDirectory);
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(store.GetPath(new DateOnly(2026, 6, 16)), "{ broken");

        var result = store.Load(new DateOnly(2026, 6, 16));

        Assert.False(result.IsSuccess);
        Assert.Empty(result.History.Domains);
        Assert.Contains("invalid JSON", result.State.Status);
    }

    [Fact]
    public void GetPath_UsesSeparateFilePerDay()
    {
        var store = new TodayTrafficHistoryStore(tempDirectory);

        var first = store.GetPath(new DateOnly(2026, 6, 16));
        var second = store.GetPath(new DateOnly(2026, 6, 17));

        Assert.EndsWith("2026-06-16.json", first);
        Assert.EndsWith("2026-06-17.json", second);
        Assert.NotEqual(first, second);
    }
}

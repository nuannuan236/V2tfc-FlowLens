using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class TrafficHistoryBrowserTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "FlowLensHistoryBrowserTests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ListDates_ReturnsJsonDatesDescending()
    {
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(Path.Combine(tempDirectory, "2026-06-15.json"), "{}");
        File.WriteAllText(Path.Combine(tempDirectory, "2026-06-17.json"), "{}");
        File.WriteAllText(Path.Combine(tempDirectory, "not-a-date.json"), "{}");
        var browser = new TrafficHistoryBrowser(new TodayTrafficHistoryStore(tempDirectory));

        var dates = browser.ListDates();

        Assert.Equal([new DateOnly(2026, 6, 17), new DateOnly(2026, 6, 15)], dates.Select(date => date.Date).ToArray());
    }

    [Fact]
    public void Load_ReadsSelectedDay()
    {
        var store = new TodayTrafficHistoryStore(tempDirectory);
        var date = new DateOnly(2026, 6, 16);
        store.Save(new TodayTrafficHistory(
            date,
            [new ApplicationTrafficSummary("msedge.exe", 10, 1, 1, 0, 0, 0, 123, 123, 0, 0, new DateTime(2026, 6, 16, 8, 0, 0))],
            [new DomainTrafficSummary("github.com", 1, "msedge.exe", 1, 0, 0, 123, 123, 0, 0, new DateTime(2026, 6, 16, 8, 0, 0))]));
        var browser = new TrafficHistoryBrowser(store);

        var result = browser.Load(date);

        Assert.True(result.IsSuccess);
        Assert.Equal(123, Assert.Single(result.History.Applications).TotalBytes);
        Assert.Equal("github.com", Assert.Single(result.History.Domains).Domain);
    }

    [Fact]
    public void Load_DamagedJsonReturnsSafeFailure()
    {
        var store = new TodayTrafficHistoryStore(tempDirectory);
        var date = new DateOnly(2026, 6, 16);
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(store.GetPath(date), "{ broken");
        var browser = new TrafficHistoryBrowser(store);

        var result = browser.Load(date);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.History.Applications);
        Assert.Contains("invalid JSON", result.State.Status);
    }
}

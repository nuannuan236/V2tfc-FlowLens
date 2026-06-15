using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void SaveAndLoad_PreservesSettings()
    {
        using var tempDirectory = new TempDirectory();
        var settingsPath = Path.Combine(tempDirectory.Path, "settings.json");
        var store = new SettingsStore(settingsPath);
        var settings = new FlowLensSettings
        {
            LogPath = @"E:\tools\v2rayN",
            ProxyPorts = new HashSet<int> { 10808, 10810 },
            RefreshIntervalSeconds = 5,
            HideCoreProcesses = false,
            OnlyShowProxy = true,
            MinimizeToTray = false,
            StartMinimized = true
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings.LogPath, loaded.LogPath);
        Assert.Equal(settings.ProxyPorts.Order(), loaded.ProxyPorts.Order());
        Assert.Equal(5, loaded.RefreshIntervalSeconds);
        Assert.False(loaded.HideCoreProcesses);
        Assert.True(loaded.OnlyShowProxy);
        Assert.False(loaded.MinimizeToTray);
        Assert.True(loaded.StartMinimized);
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileDoesNotExist()
    {
        using var tempDirectory = new TempDirectory();
        var store = new SettingsStore(Path.Combine(tempDirectory.Path, "missing.json"));

        var loaded = store.Load();

        Assert.Equal(string.Empty, loaded.LogPath);
        Assert.Equal(new[] { 10808, 10809 }, loaded.ProxyPorts.Order());
        Assert.Equal(2, loaded.RefreshIntervalSeconds);
        Assert.True(loaded.HideCoreProcesses);
        Assert.False(loaded.OnlyShowProxy);
        Assert.True(loaded.MinimizeToTray);
        Assert.False(loaded.StartMinimized);
    }

    [Fact]
    public void Load_ReturnsDefaultsWhenFileIsInvalidJson()
    {
        using var tempDirectory = new TempDirectory();
        var settingsPath = Path.Combine(tempDirectory.Path, "settings.json");
        File.WriteAllText(settingsPath, "{ not json");
        var store = new SettingsStore(settingsPath);

        var loaded = store.Load();

        Assert.Equal(new[] { 10808, 10809 }, loaded.ProxyPorts.Order());
        Assert.Equal(2, loaded.RefreshIntervalSeconds);
        Assert.True(loaded.MinimizeToTray);
        Assert.False(loaded.StartMinimized);
    }

    [Fact]
    public void Load_UsesTrayDefaultsWhenOldSettingsFileDoesNotContainTrayFields()
    {
        using var tempDirectory = new TempDirectory();
        var settingsPath = Path.Combine(tempDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "LogPath": "E:\\tools\\v2rayN",
              "ProxyPorts": [10808],
              "RefreshIntervalSeconds": 3,
              "HideCoreProcesses": true,
              "OnlyShowProxy": false
            }
            """);
        var store = new SettingsStore(settingsPath);

        var loaded = store.Load();

        Assert.True(loaded.MinimizeToTray);
        Assert.False(loaded.StartMinimized);
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"flowlens-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}

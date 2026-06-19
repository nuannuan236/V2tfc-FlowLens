using System.Text.Json;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string path;

    public SettingsStore()
        : this(GetDefaultPath())
    {
    }

    public SettingsStore(string path)
    {
        this.path = path;
    }

    public string Path => path;

    public FlowLensSettings Load()
    {
        try
        {
            if (!File.Exists(path))
            {
                return new FlowLensSettings();
            }

            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions);
            return Normalize(dto);
        }
        catch (JsonException)
        {
            return new FlowLensSettings();
        }
        catch (IOException)
        {
            return new FlowLensSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new FlowLensSettings();
        }
    }

    public void Save(FlowLensSettings settings)
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var dto = new SettingsDto
            {
                LogPath = settings.LogPath,
                ProxyPorts = settings.ProxyPorts.Order().ToArray(),
                RefreshIntervalSeconds = settings.RefreshIntervalSeconds,
                HideCoreProcesses = settings.HideCoreProcesses,
                OnlyShowProxy = settings.OnlyShowProxy,
                AttributionMode = settings.AttributionMode.ToString(),
                MinimizeToTray = settings.MinimizeToTray,
                StartMinimized = settings.StartMinimized,
                UiLanguage = NormalizeUiLanguage(settings.UiLanguage)
            };

            File.WriteAllText(path, JsonSerializer.Serialize(dto, JsonOptions));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static FlowLensSettings Normalize(SettingsDto? dto)
    {
        if (dto is null)
        {
            return new FlowLensSettings();
        }

        var ports = dto.ProxyPorts
            .Where(port => port is > 0 and <= 65535)
            .ToHashSet();

        return new FlowLensSettings
        {
            LogPath = dto.LogPath?.Trim() ?? string.Empty,
            ProxyPorts = ports.Count == 0 ? new HashSet<int> { 10808, 10809 } : ports,
            RefreshIntervalSeconds = dto.RefreshIntervalSeconds > 0 ? dto.RefreshIntervalSeconds : 2,
            HideCoreProcesses = dto.HideCoreProcesses,
            OnlyShowProxy = dto.OnlyShowProxy,
            AttributionMode = ParseAttributionMode(dto.AttributionMode),
            MinimizeToTray = dto.MinimizeToTray,
            StartMinimized = dto.StartMinimized,
            UiLanguage = NormalizeUiLanguage(dto.UiLanguage)
        };
    }

    private static AttributionMode ParseAttributionMode(string? value)
    {
        return Enum.TryParse<AttributionMode>(value, ignoreCase: true, out var mode)
            ? mode
            : AttributionMode.NormalProxy;
    }

    private static string NormalizeUiLanguage(string? value)
    {
        return string.Equals(value, "简体中文", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "SimplifiedChinese", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "zh-CN", StringComparison.OrdinalIgnoreCase)
            ? "简体中文"
            : "English";
    }

    private static string GetDefaultPath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return System.IO.Path.Combine(localAppData, "V2rayN.FlowLens", "settings.json");
    }

    private sealed class SettingsDto
    {
        public string? LogPath { get; init; }

        public int[] ProxyPorts { get; init; } = [];

        public int RefreshIntervalSeconds { get; init; } = 2;

        public bool HideCoreProcesses { get; init; } = true;

        public bool OnlyShowProxy { get; init; }

        public string? AttributionMode { get; init; }

        public bool MinimizeToTray { get; init; } = true;

        public bool StartMinimized { get; init; }

        public string? UiLanguage { get; init; }
    }
}

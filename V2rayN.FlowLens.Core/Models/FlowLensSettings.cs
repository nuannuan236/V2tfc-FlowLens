namespace V2rayN.FlowLens.Core.Models;

public sealed record FlowLensSettings
{
    public string LogPath { get; init; } = string.Empty;

    public IReadOnlySet<int> ProxyPorts { get; init; } = new HashSet<int> { 10808, 10809 };

    public int RefreshIntervalSeconds { get; init; } = 2;

    public bool HideCoreProcesses { get; init; } = true;

    public bool OnlyShowProxy { get; init; }

    public AttributionMode AttributionMode { get; init; } = AttributionMode.NormalProxy;

    public bool MinimizeToTray { get; init; } = true;

    public bool StartMinimized { get; init; }

    public string UiLanguage { get; init; } = "English";
}

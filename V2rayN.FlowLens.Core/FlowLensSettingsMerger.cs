using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public static class FlowLensSettingsMerger
{
    public static FlowLensSettings MergeDiscoveredSettings(
        FlowLensSettings settings,
        V2rayNConfigDiscoveryResult discoveryResult)
    {
        var ports = settings.ProxyPorts.ToHashSet();
        if (discoveryResult.Status == V2rayNConfigDiscoveryStatus.Found)
        {
            ports.UnionWith(discoveryResult.CandidatePorts);
        }

        var logPath = settings.LogPath;
        if (discoveryResult.Status == V2rayNConfigDiscoveryStatus.Found && !string.IsNullOrWhiteSpace(discoveryResult.RootDirectory))
        {
            logPath = discoveryResult.RootDirectory;
        }

        return settings with
        {
            LogPath = logPath,
            ProxyPorts = ports.Count == 0 ? new HashSet<int> { 10808, 10809 } : ports
        };
    }
}

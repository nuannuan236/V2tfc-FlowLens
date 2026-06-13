using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class FlowLensSettingsMergerTests
{
    [Fact]
    public void MergeDiscoveredSettings_AppendsDiscoveredPorts()
    {
        var settings = new FlowLensSettings
        {
            LogPath = string.Empty,
            ProxyPorts = new HashSet<int> { 10809 }
        };
        var discovery = new V2rayNConfigDiscoveryResult(
            V2rayNConfigDiscoveryStatus.Found,
            @"E:\tools\v2rayN",
            new HashSet<int> { 10808 },
            "found");

        var merged = FlowLensSettingsMerger.MergeDiscoveredSettings(settings, discovery);

        Assert.Equal(@"E:\tools\v2rayN", merged.LogPath);
        Assert.Equal([10808, 10809], merged.ProxyPorts.Order());
    }

    [Fact]
    public void MergeDiscoveredSettings_KeepsUserPortsWhenConfigParseFails()
    {
        var settings = new FlowLensSettings
        {
            LogPath = @"E:\custom",
            ProxyPorts = new HashSet<int> { 10810 }
        };
        var discovery = new V2rayNConfigDiscoveryResult(
            V2rayNConfigDiscoveryStatus.ParseFailed,
            @"E:\tools\v2rayN",
            new HashSet<int>(),
            "invalid config");

        var merged = FlowLensSettingsMerger.MergeDiscoveredSettings(settings, discovery);

        Assert.Equal(@"E:\custom", merged.LogPath);
        Assert.Equal([10810], merged.ProxyPorts.Order());
    }
}

using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class ConnectionSnapshotCacheTests
{
    [Fact]
    public void Update_KeepsProxyConnectionWithinRetentionAfterItDisappears()
    {
        var cache = new ConnectionSnapshotCache(TimeSpan.FromSeconds(120));
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808 }
        };
        var connection = new TcpConnectionInfo("127.0.0.1", 9852, "127.0.0.1", 10808, 1234, "chrome.exe", "Established");
        var now = new DateTime(2026, 6, 13, 16, 40, 0);

        var first = cache.Update([connection], settings, now);
        var second = cache.Update([], settings, now.AddSeconds(30));

        Assert.True(Assert.Single(first).IsCurrent);
        var cached = Assert.Single(second);
        Assert.False(cached.IsCurrent);
        Assert.Equal(9852, cached.Connection.LocalPort);
    }

    [Fact]
    public void Update_DropsProxyConnectionAfterRetention()
    {
        var cache = new ConnectionSnapshotCache(TimeSpan.FromSeconds(120));
        var settings = new FlowLensSettings
        {
            ProxyPorts = new HashSet<int> { 10808 }
        };
        var connection = new TcpConnectionInfo("127.0.0.1", 9852, "127.0.0.1", 10808, 1234, "chrome.exe", "Established");
        var now = new DateTime(2026, 6, 13, 16, 40, 0);

        cache.Update([connection], settings, now);
        var snapshots = cache.Update([], settings, now.AddSeconds(121));

        Assert.Empty(snapshots);
    }
}

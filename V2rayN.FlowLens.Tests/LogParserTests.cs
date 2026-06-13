using V2rayN.FlowLens.Core;

namespace V2rayN.FlowLens.Tests;

public sealed class LogParserTests
{
    [Theory]
    [InlineData("2026/06/13 16:39:10 from 127.0.0.1:9852 accepted //www.google-analytics.com:443 [socks -> proxy]", "socks", "proxy", "www.google-analytics.com", 443)]
    [InlineData("2026/06/13 16:39:11 from 127.0.0.1:9853 accepted //github.com:443 [http -> proxy]", "http", "proxy", "github.com", 443)]
    [InlineData("2026/06/13 16:39:12 from 127.0.0.1:9854 accepted //www.google.com:443 [mixed -> proxy]", "mixed", "proxy", "www.google.com", 443)]
    [InlineData("2026/06/13 16:39:13 from 127.0.0.1:9855 accepted //example.cn:443 [socks -> direct]", "socks", "direct", "example.cn", 443)]
    [InlineData("2026/06/13 16:39:14 from 127.0.0.1:9856 accepted tcp:github.com:443 [socks -> proxy]", "socks", "proxy", "github.com", 443)]
    [InlineData("2026/06/13 16:39:15 from 127.0.0.1:9857 accepted udp:dns.google:53 [mixed -> custom-outbound]", "mixed", "custom-outbound", "dns.google", 53)]
    [InlineData("2026/06/13 22:13:14.573031 from 127.0.0.1:4555 accepted //acrobat.adobe.com:443 [socks >> proxy]", "socks", "proxy", "acrobat.adobe.com", 443)]
    [InlineData("2026/06/13 21:42:22.417352 from tcp:127.0.0.1:12004 accepted tcp:www.google.com:443 [socks -> proxy]", "socks", "proxy", "www.google.com", 443)]
    public void TryParse_ParsesAcceptedRouteVariants(
        string line,
        string expectedInbound,
        string expectedOutbound,
        string expectedHost,
        int expectedPort)
    {
        var parser = new LogParser();

        var parsed = parser.TryParse(line, out var record);

        Assert.True(parsed);
        Assert.NotNull(record);
        Assert.Equal("127.0.0.1", record.SourceAddress);
        Assert.Equal(expectedHost, record.TargetHost);
        Assert.Equal(expectedPort, record.TargetPort);
        Assert.Equal(expectedInbound, record.Inbound);
        Assert.Equal(expectedOutbound, record.Outbound);
    }

    [Fact]
    public void TryParse_ParsesAcceptedProxyLine()
    {
        var parser = new LogParser();
        var line = "2026/06/13 16:39:10 from 127.0.0.1:9852 accepted //www.google-analytics.com:443 [socks -> proxy]";

        var parsed = parser.TryParse(line, out var record);

        Assert.True(parsed);
        Assert.NotNull(record);
        Assert.Equal(new DateTime(2026, 6, 13, 16, 39, 10), record.Timestamp);
        Assert.Equal("127.0.0.1", record.SourceAddress);
        Assert.Equal(9852, record.SourcePort);
        Assert.Equal("www.google-analytics.com", record.TargetHost);
        Assert.Equal(443, record.TargetPort);
        Assert.Equal("socks", record.Inbound);
        Assert.Equal("proxy", record.Outbound);
    }

    [Fact]
    public void TryParse_ParsesAcceptedRouteWhenLineHasPrefix()
    {
        var parser = new LogParser();
        var line = "[Info] 2026/06/13 16:39:10 from 127.0.0.1:9852 accepted tcp:github.com:443 [http -> proxy]";

        var parsed = parser.TryParse(line, out var record);

        Assert.True(parsed);
        Assert.NotNull(record);
        Assert.Equal(9852, record.SourcePort);
        Assert.Equal("github.com", record.TargetHost);
        Assert.Equal(443, record.TargetPort);
        Assert.Equal("http", record.Inbound);
        Assert.Equal("proxy", record.Outbound);
    }

    [Fact]
    public void TryParse_ReturnsFalseForUnsupportedLine()
    {
        var parser = new LogParser();

        var parsed = parser.TryParse("this is not a supported v2rayN route log", out var record);

        Assert.False(parsed);
        Assert.Null(record);
    }
}

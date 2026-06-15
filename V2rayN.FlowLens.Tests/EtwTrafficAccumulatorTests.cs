using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Tests;

public sealed class EtwTrafficAccumulatorTests
{
    [Fact]
    public void RecordSend_UsesExactProcessKey()
    {
        var accumulator = new EtwTrafficAccumulator();

        accumulator.RecordSend(1234, "127.0.0.1", 50000, "127.0.0.1", 10808, 100);

        var traffic = Assert.Single(accumulator.GetSnapshot());
        Assert.Equal(new TrafficFlowKey(1234, "127.0.0.1", 50000, "127.0.0.1", 10808), traffic.Key);
        Assert.Equal(100, traffic.Value.SentBytes);
        Assert.Equal(0, traffic.Value.ReceivedBytes);
    }

    [Fact]
    public void RecordReceive_FallsBackToUniqueActiveFourTupleWhenProcessIdDiffers()
    {
        var accumulator = new EtwTrafficAccumulator();
        var appKey = new TrafficFlowKey(1234, "127.0.0.1", 50000, "127.0.0.1", 10808);
        accumulator.RetainKeys([appKey]);

        accumulator.RecordReceive(9999, "127.0.0.1", 50000, "127.0.0.1", 10808, 250);

        var traffic = Assert.Single(accumulator.GetSnapshot());
        Assert.Equal(appKey, traffic.Key);
        Assert.Equal(0, traffic.Value.SentBytes);
        Assert.Equal(250, traffic.Value.ReceivedBytes);
    }

    [Fact]
    public void RecordReceive_DropsBytesWhenNoActiveFourTupleMatches()
    {
        var accumulator = new EtwTrafficAccumulator();
        accumulator.RetainKeys([new TrafficFlowKey(1234, "127.0.0.1", 50000, "127.0.0.1", 10808)]);

        accumulator.RecordReceive(9999, "127.0.0.1", 50001, "127.0.0.1", 10808, 250);

        Assert.Empty(accumulator.GetSnapshot());
    }

    [Fact]
    public void RecordReceive_DropsBytesWhenActiveFourTupleIsAmbiguous()
    {
        var accumulator = new EtwTrafficAccumulator();
        accumulator.RetainKeys(
        [
            new TrafficFlowKey(1234, "127.0.0.1", 50000, "127.0.0.1", 10808),
            new TrafficFlowKey(5678, "127.0.0.1", 50000, "127.0.0.1", 10808)
        ]);

        accumulator.RecordReceive(9999, "127.0.0.1", 50000, "127.0.0.1", 10808, 250);

        Assert.Empty(accumulator.GetSnapshot());
    }
}

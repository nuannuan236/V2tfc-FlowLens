using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class EtwTrafficAccumulator
{
    private readonly object _gate = new();
    private readonly Dictionary<TrafficFlowKey, TrafficCounters> _traffic = [];
    private HashSet<TrafficFlowKey> _activeKeys = [];

    public void RecordSend(
        int processId,
        string localAddress,
        int localPort,
        string remoteAddress,
        int remotePort,
        long bytes)
    {
        var key = new TrafficFlowKey(processId, localAddress, localPort, remoteAddress, remotePort);
        AddBytes(key, sentBytes: bytes, receivedBytes: 0);
    }

    public void RecordReceive(
        int processId,
        string localAddress,
        int localPort,
        string remoteAddress,
        int remotePort,
        long bytes)
    {
        var exactKey = new TrafficFlowKey(processId, localAddress, localPort, remoteAddress, remotePort);
        var targetKey = ResolveReceiveKey(exactKey);
        if (targetKey is null)
        {
            return;
        }

        AddBytes(targetKey, sentBytes: 0, receivedBytes: bytes);
    }

    public IReadOnlyDictionary<TrafficFlowKey, TrafficCounters> GetSnapshot()
    {
        lock (_gate)
        {
            return _traffic.ToDictionary(pair => pair.Key, pair => pair.Value);
        }
    }

    public void RetainKeys(IEnumerable<TrafficFlowKey> activeKeys)
    {
        lock (_gate)
        {
            _activeKeys = activeKeys.ToHashSet();
            foreach (var key in _traffic.Keys.ToArray())
            {
                if (!_activeKeys.Contains(key))
                {
                    _traffic.Remove(key);
                }
            }
        }
    }

    private TrafficFlowKey? ResolveReceiveKey(TrafficFlowKey exactKey)
    {
        lock (_gate)
        {
            if (_activeKeys.Contains(exactKey) || _traffic.ContainsKey(exactKey))
            {
                return exactKey;
            }

            var matchingActiveKeys = _activeKeys
                .Where(key =>
                    key.LocalAddress.Equals(exactKey.LocalAddress, StringComparison.OrdinalIgnoreCase) &&
                    key.LocalPort == exactKey.LocalPort &&
                    key.RemoteAddress.Equals(exactKey.RemoteAddress, StringComparison.OrdinalIgnoreCase) &&
                    key.RemotePort == exactKey.RemotePort)
                .Take(2)
                .ToArray();

            return matchingActiveKeys.Length == 1 ? matchingActiveKeys[0] : null;
        }
    }

    private void AddBytes(TrafficFlowKey key, long sentBytes, long receivedBytes)
    {
        lock (_gate)
        {
            if (_traffic.TryGetValue(key, out var current))
            {
                _traffic[key] = new TrafficCounters(
                    current.SentBytes + sentBytes,
                    current.ReceivedBytes + receivedBytes);
                return;
            }

            _traffic[key] = new TrafficCounters(sentBytes, receivedBytes);
        }
    }
}

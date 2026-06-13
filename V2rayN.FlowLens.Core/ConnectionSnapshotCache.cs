using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class ConnectionSnapshotCache(TimeSpan? retention = null)
{
    private readonly TimeSpan _retention = retention ?? TimeSpan.FromSeconds(120);
    private readonly Dictionary<ConnectionKey, CachedConnection> _connections = [];

    public IReadOnlyList<ConnectionSnapshot> Update(
        IEnumerable<TcpConnectionInfo> currentConnections,
        FlowLensSettings settings,
        DateTime now)
    {
        var currentKeys = new HashSet<ConnectionKey>();

        foreach (var connection in currentConnections)
        {
            if (!settings.ProxyPorts.Contains(connection.RemotePort) || !IsLoopback(connection.RemoteAddress))
            {
                continue;
            }

            var key = ConnectionKey.From(connection);
            currentKeys.Add(key);
            _connections[key] = new CachedConnection(connection, now);
        }

        foreach (var key in _connections
                     .Where(pair => now - pair.Value.LastSeen > _retention)
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _connections.Remove(key);
        }

        return _connections
            .Values
            .Select(cached => new ConnectionSnapshot(cached.Connection, cached.LastSeen, currentKeys.Contains(ConnectionKey.From(cached.Connection))))
            .OrderByDescending(snapshot => snapshot.LastSeen)
            .ToArray();
    }

    private static bool IsLoopback(string address)
    {
        return address.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CachedConnection(TcpConnectionInfo Connection, DateTime LastSeen);

    private sealed record ConnectionKey(
        int ProcessId,
        string LocalAddress,
        int LocalPort,
        string RemoteAddress,
        int RemotePort)
    {
        public static ConnectionKey From(TcpConnectionInfo connection)
        {
            return new ConnectionKey(
                connection.ProcessId,
                connection.LocalAddress,
                connection.LocalPort,
                connection.RemoteAddress,
                connection.RemotePort);
        }
    }
}

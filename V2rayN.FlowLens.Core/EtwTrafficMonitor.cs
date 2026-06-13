using System.Collections.Concurrent;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class EtwTrafficMonitor : IDisposable
{
    private readonly ConcurrentDictionary<TrafficFlowKey, TrafficCounters> _traffic = [];
    private readonly object _gate = new();
    private HashSet<int> _proxyPorts = [];
    private TraceEventSession? _session;
    private Task? _processingTask;
    private bool _disposed;

    public string Status { get; private set; } = "Not started";

    public void StartOrUpdate(IReadOnlySet<int> proxyPorts)
    {
        lock (_gate)
        {
            _proxyPorts = proxyPorts.ToHashSet();

            if (_session is not null || _disposed)
            {
                return;
            }

            if (!OperatingSystem.IsWindows())
            {
                Status = "ETW traffic unavailable: Windows is required.";
                return;
            }

            if (!WindowsPrivilege.IsAdministrator())
            {
                Status = "ETW traffic unavailable: run FlowLens as administrator.";
                return;
            }

            try
            {
                var sessionName = $"V2rayN.FlowLens.Traffic.{Environment.ProcessId}";
                _session = new TraceEventSession(sessionName)
                {
                    StopOnDispose = true
                };
                _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
                _session.Source.Kernel.TcpIpSend += OnTcpIpSend;
                _session.Source.Kernel.TcpIpRecv += OnTcpIpRecv;
                _processingTask = Task.Run(ProcessSession);
                Status = "ETW traffic running.";
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                _session?.Dispose();
                _session = null;
                Status = $"ETW traffic unavailable: {ex.Message}";
            }
        }
    }

    public IReadOnlyDictionary<TrafficFlowKey, TrafficCounters> GetSnapshot()
    {
        return _traffic.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public void RetainKeys(IEnumerable<TrafficFlowKey> activeKeys)
    {
        var activeKeySet = activeKeys.ToHashSet();
        foreach (var key in _traffic.Keys)
        {
            if (!activeKeySet.Contains(key))
            {
                _traffic.TryRemove(key, out _);
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _session?.Dispose();
            _session = null;
        }

        try
        {
            _processingTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }
    }

    private void ProcessSession()
    {
        try
        {
            _session?.Source.Process();
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            lock (_gate)
            {
                if (!_disposed)
                {
                    Status = $"ETW traffic stopped: {ex.Message}";
                }
            }
        }
    }

    private void OnTcpIpSend(TcpIpSendTraceData data)
    {
        var sourceAddress = data.saddr.ToString();
        var remoteAddress = data.daddr.ToString();
        if (!IsLoopback(remoteAddress) || !IsProxyPort(data.dport))
        {
            return;
        }

        var key = new TrafficFlowKey(data.ProcessID, sourceAddress, data.sport, remoteAddress, data.dport);
        AddBytes(key, sentBytes: data.size, receivedBytes: 0);
    }

    private void OnTcpIpRecv(TcpIpTraceData data)
    {
        var sourceAddress = data.saddr.ToString();
        var localAddress = data.daddr.ToString();
        if (!IsLoopback(sourceAddress) || !IsProxyPort(data.sport))
        {
            return;
        }

        var key = new TrafficFlowKey(data.ProcessID, localAddress, data.dport, sourceAddress, data.sport);
        AddBytes(key, sentBytes: 0, receivedBytes: data.size);
    }

    private void AddBytes(TrafficFlowKey key, long sentBytes, long receivedBytes)
    {
        _traffic.AddOrUpdate(
            key,
            _ => new TrafficCounters(sentBytes, receivedBytes),
            (_, current) => new TrafficCounters(current.SentBytes + sentBytes, current.ReceivedBytes + receivedBytes));
    }

    private bool IsProxyPort(int port)
    {
        lock (_gate)
        {
            return _proxyPorts.Contains(port);
        }
    }

    private static bool IsLoopback(string address)
    {
        return address.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

}

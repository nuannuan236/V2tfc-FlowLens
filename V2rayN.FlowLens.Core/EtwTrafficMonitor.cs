using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.Core;

public sealed class EtwTrafficMonitor : IDisposable
{
    private readonly EtwTrafficAccumulator _traffic = new();
    private readonly object _gate = new();
    private HashSet<int> _proxyPorts = [];
    private bool _captureAllTcp;
    private TraceEventSession? _session;
    private Task? _processingTask;
    private bool _disposed;

    public string Status { get; private set; } = "Not started";

    public void StartOrUpdate(IReadOnlySet<int> proxyPorts, bool captureAllTcp = false)
    {
        lock (_gate)
        {
            _proxyPorts = proxyPorts.ToHashSet();
            _captureAllTcp = captureAllTcp;

            if (_session is not null || _disposed)
            {
                return;
            }

            if (!OperatingSystem.IsWindows())
            {
                Status = "Unavailable: Windows is required.";
                return;
            }

            if (!WindowsPrivilege.IsAdministrator())
            {
                Status = "Needs administrator: run FlowLens as administrator for ETW traffic.";
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
                Status = "Running";
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                _session?.Dispose();
                _session = null;
                Status = $"Unavailable: {ex.Message}";
            }
        }
    }

    public IReadOnlyDictionary<TrafficFlowKey, TrafficCounters> GetSnapshot()
    {
        return _traffic.GetSnapshot();
    }

    public void RetainKeys(IEnumerable<TrafficFlowKey> activeKeys)
    {
        _traffic.RetainKeys(activeKeys);
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
                    Status = $"Unavailable: ETW traffic stopped: {ex.Message}";
                }
            }
        }
    }

    private void OnTcpIpSend(TcpIpSendTraceData data)
    {
        var sourceAddress = data.saddr.ToString();
        var remoteAddress = data.daddr.ToString();
        if (!ShouldCaptureSend(remoteAddress, data.dport))
        {
            return;
        }

        _traffic.RecordSend(data.ProcessID, sourceAddress, data.sport, remoteAddress, data.dport, data.size);
    }

    private void OnTcpIpRecv(TcpIpTraceData data)
    {
        var sourceAddress = data.saddr.ToString();
        var localAddress = data.daddr.ToString();
        if (!ShouldCaptureReceive(sourceAddress, data.sport))
        {
            return;
        }

        _traffic.RecordReceive(data.ProcessID, localAddress, data.dport, sourceAddress, data.sport, data.size);
    }

    private bool ShouldCaptureSend(string remoteAddress, int remotePort)
    {
        lock (_gate)
        {
            return _captureAllTcp || (IsLoopback(remoteAddress) && _proxyPorts.Contains(remotePort));
        }
    }

    private bool ShouldCaptureReceive(string sourceAddress, int sourcePort)
    {
        lock (_gate)
        {
            return _captureAllTcp || (IsLoopback(sourceAddress) && _proxyPorts.Contains(sourcePort));
        }
    }

    private static bool IsLoopback(string address)
    {
        return address.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

}

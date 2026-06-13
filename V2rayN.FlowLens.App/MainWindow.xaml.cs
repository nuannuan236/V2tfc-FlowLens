using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.App;

public partial class MainWindow : Window
{
    private readonly LogFileReader _logFileReader = new(new LogParser());
    private readonly LogHealthChecker _logHealthChecker = new(new LogParser());
    private readonly WindowsTcpConnectionReader _tcpConnectionReader = new();
    private readonly ConnectionSnapshotCache _connectionSnapshotCache = new();
    private readonly EtwTrafficMonitor _trafficMonitor = new();
    private readonly AttributionEngine _attributionEngine = new();
    private readonly DispatcherTimer _refreshTimer = new();

    public ObservableCollection<ApplicationTrafficSummary> ApplicationSummaries { get; } = [];

    public ObservableCollection<AttributedConnection> AttributedConnections { get; } = [];

    public ObservableCollection<DomainTrafficSummary> DomainSummaries { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        _refreshTimer.Tick += (_, _) => RefreshData();
        ApplyRefreshInterval();
        _refreshTimer.Start();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyRefreshInterval();
        RefreshData();
    }

    private void RefreshData()
    {
        try
        {
            var settings = ReadSettings();
            var logHealthStatus = _logHealthChecker.Check(settings.LogPath);
            var logReadResult = _logFileReader.ReadRecentWithInfo(settings.LogPath);
            _trafficMonitor.StartOrUpdate(settings.ProxyPorts);
            var tcpConnections = _tcpConnectionReader.Read();
            var connectionSnapshots = _connectionSnapshotCache.Update(tcpConnections, settings, DateTime.Now);
            _trafficMonitor.RetainKeys(connectionSnapshots.Select(ToTrafficFlowKey));
            var attributedConnections = _attributionEngine.Attribute(
                connectionSnapshots,
                logReadResult.Records,
                settings,
                _trafficMonitor.GetSnapshot(),
                DateTime.Now);
            var applicationSummaries = _attributionEngine.SummarizeApplications(attributedConnections);
            var domainSummaries = _attributionEngine.SummarizeDomains(attributedConnections);

            Replace(ApplicationSummaries, applicationSummaries);
            Replace(AttributedConnections, attributedConnections);
            Replace(DomainSummaries, domainSummaries);

            UpdateLogHealthWarning(logHealthStatus);
            StatusTextBlock.Text = $"Updated {DateTime.Now:HH:mm:ss}. Log health: {logHealthStatus}. {_trafficMonitor.Status} Logs: {logReadResult.Records.Count}. TCP rows: {tcpConnections.Count}. Cached: {connectionSnapshots.Count}. Attributed: {attributedConnections.Count}. Active logs: {FormatLogFiles(logReadResult.Files)}";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Refresh failed: {ex.Message}";
        }
    }

    private void UpdateLogHealthWarning(LogHealthStatus status)
    {
        LogHealthWarningBorder.Visibility = status == LogHealthStatus.NoCoreRoutingLog
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private FlowLensSettings ReadSettings()
    {
        return new FlowLensSettings
        {
            LogPath = LogPathTextBox.Text.Trim(),
            ProxyPorts = ParsePorts(ProxyPortsTextBox.Text),
            RefreshIntervalSeconds = ParseRefreshInterval(RefreshSecondsTextBox.Text),
            HideCoreProcesses = HideCoreProcessesCheckBox.IsChecked == true,
            OnlyShowProxy = OnlyProxyCheckBox.IsChecked == true
        };
    }

    private void ApplyRefreshInterval()
    {
        _refreshTimer.Interval = TimeSpan.FromSeconds(ParseRefreshInterval(RefreshSecondsTextBox.Text));
    }

    private static IReadOnlySet<int> ParsePorts(string value)
    {
        var ports = value
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(portText => int.TryParse(portText, out var port) ? port : 0)
            .Where(port => port is > 0 and <= 65535)
            .ToHashSet();

        return ports.Count == 0 ? new HashSet<int> { 10808, 10809 } : ports;
    }

    private static int ParseRefreshInterval(string value)
    {
        return int.TryParse(value, out var seconds) && seconds > 0 ? seconds : 2;
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private static string FormatLogFiles(IReadOnlyList<string> files)
    {
        return files.Count == 0
            ? "none"
            : string.Join("; ", files.Take(3));
    }

    private static TrafficFlowKey ToTrafficFlowKey(ConnectionSnapshot snapshot)
    {
        var connection = snapshot.Connection;
        return new TrafficFlowKey(
            connection.ProcessId,
            connection.LocalAddress,
            connection.LocalPort,
            connection.RemoteAddress,
            connection.RemotePort);
    }

    protected override void OnClosed(EventArgs e)
    {
        _trafficMonitor.Dispose();
        base.OnClosed(e);
    }
}

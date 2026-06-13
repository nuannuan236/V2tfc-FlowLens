using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using V2rayN.FlowLens.Core;
using V2rayN.FlowLens.Core.Models;

namespace V2rayN.FlowLens.App;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly LogFileReader _logFileReader = new(new LogParser());
    private readonly LogHealthChecker _logHealthChecker = new(new LogParser());
    private readonly WindowsTcpConnectionReader _tcpConnectionReader = new();
    private readonly ConnectionSnapshotCache _connectionSnapshotCache = new();
    private readonly EtwTrafficMonitor _trafficMonitor = new();
    private readonly AttributionEngine _attributionEngine = new();
    private readonly FlowLensDiagnosticBuilder _diagnosticBuilder = new();
    private readonly V2rayNConfigDiscovery _configDiscovery = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly DispatcherTimer _refreshTimer = new();
    private bool _isLoadingSettings;
    private FlowLensDiagnostics _currentDiagnostics = new();

    public ObservableCollection<ApplicationTrafficSummary> ApplicationSummaries { get; } = [];

    public ObservableCollection<AttributedConnection> AttributedConnections { get; } = [];

    public ObservableCollection<DomainTrafficSummary> DomainSummaries { get; } = [];

    public FlowLensDiagnostics CurrentDiagnostics
    {
        get => _currentDiagnostics;
        private set
        {
            if (_currentDiagnostics == value)
            {
                return;
            }

            _currentDiagnostics = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadSettingsIntoUi(_settingsStore.Load());
        AttachSettingsChangeHandlers();

        _refreshTimer.Tick += (_, _) => RefreshData();
        ApplyRefreshInterval();
        _refreshTimer.Start();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyRefreshInterval();
        SaveCurrentSettings();
        RefreshData();
    }

    private void RefreshData()
    {
        try
        {
            var settingsFromUi = ReadSettings();
            var preliminaryLogReadResult = _logFileReader.ReadRecentWithInfo(settingsFromUi.LogPath);
            var configDiscoveryResult = _configDiscovery.Discover(settingsFromUi.LogPath, preliminaryLogReadResult.Files);
            var settings = FlowLensSettingsMerger.MergeDiscoveredSettings(settingsFromUi, configDiscoveryResult);
            SyncEffectiveSettingsToUi(settings);
            SaveSettings(settings);

            var logReadResult = _logFileReader.ReadRecentWithInfo(settings.LogPath);
            var logHealthStatus = _logHealthChecker.Check(settings.LogPath);
            _trafficMonitor.StartOrUpdate(settings.ProxyPorts);
            var tcpConnections = _tcpConnectionReader.Read();
            var connectionSnapshots = _connectionSnapshotCache.Update(tcpConnections, settings, DateTime.Now);
            _trafficMonitor.RetainKeys(connectionSnapshots.Select(ToTrafficFlowKey));
            var now = DateTime.Now;
            var attributedConnections = _attributionEngine.Attribute(
                connectionSnapshots,
                logReadResult.Records,
                settings,
                _trafficMonitor.GetSnapshot(),
                now);
            var applicationSummaries = _attributionEngine.SummarizeApplications(attributedConnections);
            var domainSummaries = _attributionEngine.SummarizeDomains(attributedConnections);

            Replace(ApplicationSummaries, applicationSummaries);
            Replace(AttributedConnections, attributedConnections);
            Replace(DomainSummaries, domainSummaries);

            CurrentDiagnostics = _diagnosticBuilder.Build(
                WindowsPrivilege.IsAdministrator(),
                _trafficMonitor.Status,
                logHealthStatus,
                configDiscoveryResult,
                settings,
                logReadResult.Files,
                tcpConnections,
                logReadResult.Records,
                attributedConnections,
                now);

            UpdateLogHealthWarning(logHealthStatus);
            UpdateEtwWarning(_trafficMonitor.Status);
            StatusTextBlock.Text = $"Updated {now:HH:mm:ss}. {CurrentDiagnostics.MatchStatsDisplay}. Logs: {logReadResult.Records.Count}. TCP rows: {tcpConnections.Count}.";
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

    private void UpdateEtwWarning(string status)
    {
        EtwWarningBorder.Visibility = status.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
            ? Visibility.Visible
            : Visibility.Collapsed;
        EtwWarningTextBlock.Text = status;
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

    private void LoadSettingsIntoUi(FlowLensSettings settings)
    {
        _isLoadingSettings = true;
        try
        {
            LogPathTextBox.Text = settings.LogPath;
            ProxyPortsTextBox.Text = FormatPorts(settings.ProxyPorts);
            RefreshSecondsTextBox.Text = settings.RefreshIntervalSeconds.ToString();
            HideCoreProcessesCheckBox.IsChecked = settings.HideCoreProcesses;
            OnlyProxyCheckBox.IsChecked = settings.OnlyShowProxy;
        }
        finally
        {
            _isLoadingSettings = false;
        }
    }

    private void AttachSettingsChangeHandlers()
    {
        LogPathTextBox.TextChanged += SettingsControl_Changed;
        ProxyPortsTextBox.TextChanged += SettingsControl_Changed;
        RefreshSecondsTextBox.TextChanged += SettingsControl_Changed;
        HideCoreProcessesCheckBox.Checked += SettingsControl_Changed;
        HideCoreProcessesCheckBox.Unchecked += SettingsControl_Changed;
        OnlyProxyCheckBox.Checked += SettingsControl_Changed;
        OnlyProxyCheckBox.Unchecked += SettingsControl_Changed;
    }

    private void SettingsControl_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        ApplyRefreshInterval();
        SaveCurrentSettings();
    }

    private void SaveCurrentSettings()
    {
        SaveSettings(ReadSettings());
    }

    private void SaveSettings(FlowLensSettings settings)
    {
        _settingsStore.Save(settings);
    }

    private void SyncEffectiveSettingsToUi(FlowLensSettings settings)
    {
        var nextLogPathText = settings.LogPath;
        var nextPortsText = FormatPorts(settings.ProxyPorts);
        if (LogPathTextBox.Text == nextLogPathText && ProxyPortsTextBox.Text == nextPortsText)
        {
            return;
        }

        _isLoadingSettings = true;
        try
        {
            LogPathTextBox.Text = nextLogPathText;
            ProxyPortsTextBox.Text = nextPortsText;
        }
        finally
        {
            _isLoadingSettings = false;
        }
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

    private static string FormatPorts(IReadOnlySet<int> ports)
    {
        return string.Join(",", ports.Order());
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
        SaveCurrentSettings();
        _trafficMonitor.Dispose();
        base.OnClosed(e);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

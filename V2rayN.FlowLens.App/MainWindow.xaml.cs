using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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
    private readonly TunAttributionEngine _tunAttributionEngine = new();
    private readonly SessionTrafficAccumulator _sessionTrafficAccumulator = new();
    private readonly SessionCsvExporter _sessionCsvExporter = new();
    private readonly TodayTrafficAccumulator _todayTrafficAccumulator = new();
    private readonly TodayTrafficHistoryStore _todayTrafficHistoryStore = new();
    private readonly TrafficHistoryBrowser _trafficHistoryBrowser = new();
    private readonly FlowLensDiagnosticBuilder _diagnosticBuilder = new();
    private readonly V2rayNConfigDiscovery _configDiscovery = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly DispatcherTimer _refreshTimer = new();
    private TrayIconController? _trayIconController;
    private bool _isLoadingSettings;
    private bool _isRefreshPaused;
    private bool _isRealExitRequested;
    private bool _hasShownTrayHint;
    private bool _minimizeToTray = true;
    private bool _startMinimized;
    private FlowLensDiagnostics _currentDiagnostics = new();
    private TunAttributionDiagnostics? _lastTunDiagnostics;
    private TodayHistoryState _todayHistoryState = new(DateOnly.FromDateTime(DateTime.Now), string.Empty, "Not loaded");
    private IReadOnlyList<ApplicationTrafficSummary> _rawApplicationSummaries = [];
    private IReadOnlyList<AttributedConnection> _rawAttributedConnections = [];
    private IReadOnlyList<DomainTrafficSummary> _rawDomainSummaries = [];
    private IReadOnlyList<ApplicationTrafficSummary> _rawSessionApplicationSummaries = [];
    private IReadOnlyList<DomainTrafficSummary> _rawSessionDomainSummaries = [];
    private IReadOnlyList<ApplicationTrafficSummary> _rawTodayApplicationSummaries = [];
    private IReadOnlyList<DomainTrafficSummary> _rawTodayDomainSummaries = [];
    private IReadOnlyList<ApplicationTrafficSummary> _rawHistoryApplicationSummaries = [];
    private IReadOnlyList<DomainTrafficSummary> _rawHistoryDomainSummaries = [];

    public ObservableCollection<ApplicationTrafficSummary> ApplicationSummaries { get; } = [];

    public ObservableCollection<AttributedConnection> AttributedConnections { get; } = [];

    public ObservableCollection<DomainTrafficSummary> DomainSummaries { get; } = [];

    public ObservableCollection<ApplicationTrafficSummary> SessionApplicationSummaries { get; } = [];

    public ObservableCollection<DomainTrafficSummary> SessionDomainSummaries { get; } = [];

    public ObservableCollection<ApplicationTrafficSummary> TodayApplicationSummaries { get; } = [];

    public ObservableCollection<DomainTrafficSummary> TodayDomainSummaries { get; } = [];

    public ObservableCollection<TrafficHistoryDateOption> HistoryDates { get; } = [];

    public ObservableCollection<ApplicationTrafficSummary> HistoryApplicationSummaries { get; } = [];

    public ObservableCollection<DomainTrafficSummary> HistoryDomainSummaries { get; } = [];

    public string RefreshStateDisplay => _isRefreshPaused ? "Paused" : "Running";

    public string TrayModeDisplay => _minimizeToTray
        ? $"Enabled. Start minimized: {(_startMinimized ? "yes" : "no")}"
        : "Disabled";

    public string SessionStartedDisplay => _sessionTrafficAccumulator.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");

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
        var settings = _settingsStore.Load();
        LoadTodayHistory(DateOnly.FromDateTime(DateTime.Now));
        LoadSettingsIntoUi(settings);
        AttachSettingsChangeHandlers();
        RefreshHistoryDates(DateOnly.FromDateTime(DateTime.Now));
        CreateTrayIcon();

        _refreshTimer.Tick += (_, _) =>
        {
            if (!_isRefreshPaused)
            {
                RefreshData();
            }
        };
        ApplyRefreshInterval();
        _refreshTimer.Start();
        UpdateRefreshControls();

        Loaded += (_, _) =>
        {
            if (_startMinimized && _minimizeToTray)
            {
                Dispatcher.BeginInvoke(() => HideToTray("FlowLens is running in the tray. Double-click the tray icon to show it."));
            }
        };
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshNow();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleRefreshPause();
    }

    private void ResetSessionButton_Click(object sender, RoutedEventArgs e)
    {
        _sessionTrafficAccumulator.Reset();
        _rawSessionApplicationSummaries = _sessionTrafficAccumulator.GetApplicationSummaries();
        _rawSessionDomainSummaries = _sessionTrafficAccumulator.GetDomainSummaries();
        ApplyFilters();
        OnPropertyChanged(nameof(SessionStartedDisplay));
        StatusTextBlock.Text = $"Refresh: {RefreshStateDisplay}. Session reset at {SessionStartedDisplay}.";
    }

    private void ExportSessionApplicationsButton_Click(object sender, RoutedEventArgs e)
    {
        ExportCsv(
            $"flowlens-session-applications-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            stream => _sessionCsvExporter.WriteApplications(stream, SessionApplicationSummaries),
            "applications");
    }

    private void ExportSessionDomainsButton_Click(object sender, RoutedEventArgs e)
    {
        ExportCsv(
            $"flowlens-session-domains-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            stream => _sessionCsvExporter.WriteDomains(stream, SessionDomainSummaries),
            "domains");
    }

    private void ExportTodayApplicationsButton_Click(object sender, RoutedEventArgs e)
    {
        ExportCsv(
            $"flowlens-today-applications-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            stream => _sessionCsvExporter.WriteApplications(stream, TodayApplicationSummaries),
            "today applications");
    }

    private void ExportTodayDomainsButton_Click(object sender, RoutedEventArgs e)
    {
        ExportCsv(
            $"flowlens-today-domains-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            stream => _sessionCsvExporter.WriteDomains(stream, TodayDomainSummaries),
            "today domains");
    }

    private void ExportHistoryApplicationsButton_Click(object sender, RoutedEventArgs e)
    {
        var date = GetSelectedHistoryDate();
        ExportCsv(
            $"flowlens-history-applications-{date:yyyyMMdd}-{DateTime.Now:HHmmss}.csv",
            stream => _sessionCsvExporter.WriteApplications(stream, HistoryApplicationSummaries),
            "history applications");
    }

    private void ExportHistoryDomainsButton_Click(object sender, RoutedEventArgs e)
    {
        var date = GetSelectedHistoryDate();
        ExportCsv(
            $"flowlens-history-domains-{date:yyyyMMdd}-{DateTime.Now:HHmmss}.csv",
            stream => _sessionCsvExporter.WriteDomains(stream, HistoryDomainSummaries),
            "history domains");
    }

    private void OpenHistoryFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenHistoryFolder();
    }

    private void CopyDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        CopyDiagnosticsReport();
    }

    private void FilterControl_Changed(object sender, RoutedEventArgs e)
    {
        ApplyFilters();
    }

    private void HistoryDateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryDateComboBox.SelectedItem is TrafficHistoryDateOption option)
        {
            LoadHistoryDate(option.Date);
        }
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
            var now = DateTime.Now;
            var isTunMode = settings.AttributionMode == AttributionMode.Tun;
            _trafficMonitor.StartOrUpdate(settings.ProxyPorts, captureAllTcp: isTunMode);
            var tcpConnections = _tcpConnectionReader.Read();
            var connectionSnapshots = isTunMode
                ? tcpConnections.Select(connection => new ConnectionSnapshot(connection, now, true)).ToArray()
                : _connectionSnapshotCache.Update(tcpConnections, settings, now);
            _trafficMonitor.RetainKeys(connectionSnapshots.Select(ToTrafficFlowKey));
            var trafficSnapshot = _trafficMonitor.GetSnapshot();
            IReadOnlyList<AttributedConnection> attributedConnections;
            if (isTunMode)
            {
                var tunResult = _tunAttributionEngine.AttributeWithDiagnostics(connectionSnapshots, logReadResult.Records, settings, trafficSnapshot, now);
                attributedConnections = tunResult.Connections;
                _lastTunDiagnostics = tunResult.Diagnostics;
            }
            else
            {
                attributedConnections = _attributionEngine.Attribute(connectionSnapshots, logReadResult.Records, settings, trafficSnapshot, now);
                _lastTunDiagnostics = null;
            }
            var applicationSummaries = _attributionEngine.SummarizeApplications(attributedConnections);
            var domainSummaries = _attributionEngine.SummarizeDomains(attributedConnections);
            _sessionTrafficAccumulator.AddSnapshot(attributedConnections);
            var sessionApplicationSummaries = _sessionTrafficAccumulator.GetApplicationSummaries();
            var sessionDomainSummaries = _sessionTrafficAccumulator.GetDomainSummaries();
            EnsureTodayHistory(now);
            _todayTrafficAccumulator.AddSnapshot(attributedConnections);
            var todayHistorySaveState = _todayTrafficHistoryStore.Save(_todayTrafficAccumulator.ToHistory());
            _todayHistoryState = todayHistorySaveState;
            var todayApplicationSummaries = _todayTrafficAccumulator.GetApplicationSummaries();
            var todayDomainSummaries = _todayTrafficAccumulator.GetDomainSummaries();

            _rawApplicationSummaries = applicationSummaries;
            _rawAttributedConnections = attributedConnections;
            _rawDomainSummaries = domainSummaries;
            _rawSessionApplicationSummaries = sessionApplicationSummaries;
            _rawSessionDomainSummaries = sessionDomainSummaries;
            _rawTodayApplicationSummaries = todayApplicationSummaries;
            _rawTodayDomainSummaries = todayDomainSummaries;
            ApplyFilters();
            RefreshHistoryDates(GetSelectedHistoryDate());
            OnPropertyChanged(nameof(SessionStartedDisplay));

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
                now,
                _todayHistoryState,
                CountTunCandidates(connectionSnapshots, settings),
                CountTunRouteEvidence(logReadResult.Records, now));

            UpdateLogHealthWarning(logHealthStatus);
            UpdateEtwWarning(_trafficMonitor.Status);
            StatusTextBlock.Text = $"Refresh: {RefreshStateDisplay}. Updated {now:HH:mm:ss}. Session started {SessionStartedDisplay}. Today: {_todayHistoryState.Status}. {CurrentDiagnostics.MatchStatsDisplay}. Logs: {logReadResult.Records.Count}. TCP rows: {tcpConnections.Count}.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Refresh: {RefreshStateDisplay}. Refresh failed: {ex.Message}";
        }
    }

    private void RefreshNow()
    {
        ApplyRefreshInterval();
        SaveCurrentSettings();
        RefreshData();
    }

    private void ExportCsv(string defaultFileName, Action<Stream> writeCsv, string label)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".csv",
                FileName = defaultFileName,
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                OverwritePrompt = true,
                Title = $"Export session {label}"
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            using var stream = File.Create(dialog.FileName);
            writeCsv(stream);
            StatusTextBlock.Text = $"Exported {label} CSV: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Export {label} CSV failed: {ex.Message}";
        }
    }

    private void EnsureTodayHistory(DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        if (_todayTrafficAccumulator.Date != today)
        {
            LoadTodayHistory(today);
        }
    }

    private void LoadTodayHistory(DateOnly date)
    {
        var loadResult = _todayTrafficHistoryStore.Load(date);
        _todayTrafficAccumulator.Load(loadResult.History);
        _todayHistoryState = loadResult.State;
        _rawTodayApplicationSummaries = _todayTrafficAccumulator.GetApplicationSummaries();
        _rawTodayDomainSummaries = _todayTrafficAccumulator.GetDomainSummaries();
        ApplyFilters();
    }

    private void RefreshHistoryDates(DateOnly preferredDate)
    {
        var dates = _trafficHistoryBrowser.ListDates();
        Replace(HistoryDates, dates);

        var selected = dates.FirstOrDefault(date => date.Date == preferredDate) ?? dates.FirstOrDefault();
        if (selected is null)
        {
            _rawHistoryApplicationSummaries = [];
            _rawHistoryDomainSummaries = [];
            ApplyFilters();
            return;
        }

        HistoryDateComboBox.SelectedItem = selected;
        LoadHistoryDate(selected.Date);
    }

    private void LoadHistoryDate(DateOnly date)
    {
        var result = _trafficHistoryBrowser.Load(date);
        _rawHistoryApplicationSummaries = result.History.Applications;
        _rawHistoryDomainSummaries = result.History.Domains;
        ApplyFilters();
        StatusTextBlock.Text = $"History {date:yyyy-MM-dd}: {result.State.Status}";
    }

    private DateOnly GetSelectedHistoryDate()
    {
        return HistoryDateComboBox.SelectedItem is TrafficHistoryDateOption option
            ? option.Date
            : DateOnly.FromDateTime(DateTime.Now);
    }

    private void ApplyFilters()
    {
        var keyword = FilterTextBox?.Text ?? string.Empty;
        var outbound = (OutboundFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";

        Replace(ApplicationSummaries, TrafficSummaryFilter.FilterApplications(_rawApplicationSummaries, keyword, outbound));
        Replace(AttributedConnections, TrafficSummaryFilter.FilterConnections(_rawAttributedConnections, keyword, outbound));
        Replace(DomainSummaries, TrafficSummaryFilter.FilterDomains(_rawDomainSummaries, keyword, outbound));
        Replace(SessionApplicationSummaries, TrafficSummaryFilter.FilterApplications(_rawSessionApplicationSummaries, keyword, outbound));
        Replace(SessionDomainSummaries, TrafficSummaryFilter.FilterDomains(_rawSessionDomainSummaries, keyword, outbound));
        Replace(TodayApplicationSummaries, TrafficSummaryFilter.FilterApplications(_rawTodayApplicationSummaries, keyword, outbound));
        Replace(TodayDomainSummaries, TrafficSummaryFilter.FilterDomains(_rawTodayDomainSummaries, keyword, outbound));
        Replace(HistoryApplicationSummaries, TrafficSummaryFilter.FilterApplications(_rawHistoryApplicationSummaries, keyword, outbound));
        Replace(HistoryDomainSummaries, TrafficSummaryFilter.FilterDomains(_rawHistoryDomainSummaries, keyword, outbound));
    }

    private void UpdateLogHealthWarning(LogHealthStatus status)
    {
        LogHealthWarningBorder.Visibility = status == LogHealthStatus.NoCoreRoutingLog
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateEtwWarning(string status)
    {
        EtwWarningBorder.Visibility = status.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            status.Contains("needs administrator", StringComparison.OrdinalIgnoreCase)
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
            OnlyShowProxy = OnlyProxyCheckBox.IsChecked == true,
            AttributionMode = ParseAttributionMode((AttributionModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()),
            MinimizeToTray = _minimizeToTray,
            StartMinimized = _startMinimized
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
            AttributionModeComboBox.SelectedIndex = settings.AttributionMode == AttributionMode.Tun ? 1 : 0;
            HideCoreProcessesCheckBox.IsChecked = settings.HideCoreProcesses;
            OnlyProxyCheckBox.IsChecked = settings.OnlyShowProxy;
            _minimizeToTray = settings.MinimizeToTray;
            _startMinimized = settings.StartMinimized;
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
        AttributionModeComboBox.SelectionChanged += SettingsControl_Changed;
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

    private void CreateTrayIcon()
    {
        _trayIconController = new TrayIconController(
            () => Dispatcher.BeginInvoke(ShowFlowLens),
            () => Dispatcher.BeginInvoke(RefreshNow),
            () => Dispatcher.BeginInvoke(ToggleRefreshPause),
            () => Dispatcher.BeginInvoke(OpenHistoryFolder),
            () => Dispatcher.BeginInvoke(CopyDiagnosticsReport),
            () => Dispatcher.BeginInvoke(RequestRealExit));
        _trayIconController.UpdatePaused(_isRefreshPaused);
    }

    private void OpenHistoryFolder()
    {
        try
        {
            Directory.CreateDirectory(_trafficHistoryBrowser.HistoryDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = _trafficHistoryBrowser.HistoryDirectory,
                UseShellExecute = true
            });
            StatusTextBlock.Text = $"Opened history folder: {_trafficHistoryBrowser.HistoryDirectory}";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Open history folder failed: {ex.Message}";
        }
    }

    private void CopyDiagnosticsReport()
    {
        try
        {
            var report = DiagnosticsReportBuilder.Build(
                CurrentDiagnostics,
                RefreshStateDisplay,
                TrayModeDisplay,
                SessionStartedDisplay,
                "V2.1",
                _lastTunDiagnostics is not null);
            System.Windows.Clipboard.SetText(report);
            StatusTextBlock.Text = "Diagnostics copied to clipboard.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Copy diagnostics failed: {ex.Message}";
        }
    }

    private void CopyTunEvidenceJsonButton_Click(object sender, RoutedEventArgs e)
    {
        CopyTunEvidenceJson();
    }

    private void CopyTunEvidenceJson()
    {
        try
        {
            var json = TunDiagnosticsJsonExporter.Serialize(_lastTunDiagnostics);
            System.Windows.Clipboard.SetText(json);
            StatusTextBlock.Text = _lastTunDiagnostics is null
                ? "No TUN diagnostics captured. Unavailable JSON copied to clipboard."
                : "TUN evidence JSON copied to clipboard.";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Copy TUN evidence JSON failed: {ex.Message}";
        }
    }

    private void ToggleRefreshPause()
    {
        _isRefreshPaused = !_isRefreshPaused;
        if (_isRefreshPaused)
        {
            _refreshTimer.Stop();
        }
        else
        {
            ApplyRefreshInterval();
            _refreshTimer.Start();
        }

        UpdateRefreshControls();
        StatusTextBlock.Text = $"Refresh: {RefreshStateDisplay}. Last refresh: {CurrentDiagnostics.LastRefreshDisplay}.";
    }

    private void UpdateRefreshControls()
    {
        PauseButton.Content = _isRefreshPaused ? "Resume" : "Pause";
        _trayIconController?.UpdatePaused(_isRefreshPaused);
        OnPropertyChanged(nameof(RefreshStateDisplay));
        OnPropertyChanged(nameof(TrayModeDisplay));
    }

    private void ShowFlowLens()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void HideToTray(string statusMessage)
    {
        if (!_hasShownTrayHint)
        {
            StatusTextBlock.Text = statusMessage;
            _hasShownTrayHint = true;
        }

        ShowInTaskbar = false;
        Hide();
    }

    private void RequestRealExit()
    {
        _isRealExitRequested = true;
        Close();
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

    private static AttributionMode ParseAttributionMode(string? value)
    {
        return Enum.TryParse<AttributionMode>(value, ignoreCase: true, out var mode)
            ? mode
            : AttributionMode.NormalProxy;
    }

    private static int CountTunCandidates(IEnumerable<ConnectionSnapshot> snapshots, FlowLensSettings settings)
    {
        if (settings.AttributionMode != AttributionMode.Tun)
        {
            return 0;
        }

        return snapshots.Count(snapshot => !IsLoopback(snapshot.Connection.RemoteAddress));
    }

    private static int CountTunRouteEvidence(IEnumerable<LogConnectionRecord> records, DateTime now)
    {
        return records.Count(record => record.Timestamp != DateTime.MinValue && now - record.Timestamp <= TimeSpan.FromSeconds(120));
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

    private static bool IsLoopback(string address)
    {
        return address.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        if (WindowState == WindowState.Minimized && _minimizeToTray && !_isRealExitRequested)
        {
            HideToTray("FlowLens is still running in the tray. Use the tray icon to show or exit.");
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isRealExitRequested && _minimizeToTray)
        {
            e.Cancel = true;
            HideToTray("FlowLens is still running in the tray. Use Exit from the tray menu to quit.");
            return;
        }

        SaveCurrentSettings();
        _refreshTimer.Stop();
        _trayIconController?.Dispose();
        _trafficMonitor.Dispose();
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

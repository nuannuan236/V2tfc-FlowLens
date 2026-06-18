# v2rayN FlowLens

v2rayN FlowLens is a Windows desktop prototype for v2rayN proxy traffic attribution.

It is a read-only monitoring tool. It does not replace v2rayN, edit v2rayN configuration, manage subscriptions or nodes, capture packets, or upload logs/domains to a remote service.

## Scope

FlowLens supports two attribution modes.

Recommended daily use:

- Use `NormalProxy` as the primary mode when v2rayN TUN is off. This is the most reliable path because FlowLens can match the application source port to access-log route records.
- Use `Tun` as an observation and diagnostics mode. It is useful for finding likely traffic-heavy applications when TUN is enabled, but results should be read with the confidence column and evidence JSON.
- Keep Core logs at `info` if you need `proxy` / `direct` route labels. Without route logs, FlowLens can still show TCP/ETW observations, but route results degrade to `unknown`.

Normal Proxy mode is the default and keeps the original precise source-port attribution:

- Read v2rayN/Xray/sing-box log files.
- Read current Windows TCP connections with PID and process name.
- Detect applications connected to configured local proxy ports, such as `10808` and `10809`.
- Match the application's local ephemeral source port to log records like `from 127.0.0.1:<port>`.
- Show application ranking, live attributed connections, domain ranking, and ETW-based byte counters.
- Show this-run session totals for applications and domains.
- Show diagnostics for admin status, ETW status, access log discovery, v2rayN config discovery, active logs, proxy ports, and match status counts.
- Show local daily aggregate history, CSV exports, lightweight filtering, and copyable diagnostics.

TUN mode is available as a conservative V2 attribution mode. It uses Windows TCP connections, ETW TCP bytes, v2rayN/Xray/sing-box route logs, a +/-5 second time window, destination IP/port, and domain evidence. It does not promise exact attribution.

## Supported Log Pattern

FlowLens needs v2rayN core routing logs for `proxy` / `direct` attribution. In v2rayN, enable Core logging, set the log level to `info`, and restart the v2rayN core before expecting route results.

The parser supports route lines like:

```text
2026/06/13 16:39:10 from 127.0.0.1:9852 accepted //www.google-analytics.com:443 [socks -> proxy]
```

Parsed fields:

- time
- source address and port
- target host and port
- inbound
- outbound: `proxy`, `direct`, `block`, or other values emitted by the core

Unsupported lines are ignored instead of treated as errors.

You can select either the v2rayN root directory or the `guiLogs` directory. FlowLens automatically checks the selected directory plus a child `guiLogs` directory and displays the actual log files it is reading.

If no path is entered, FlowLens attempts read-only discovery of a v2rayN installation and recent `Vaccess_*.txt` files. Discovery is best-effort and never changes v2rayN files.

If the selected path only contains GUI startup/error lines and no core routing records, FlowLens can still identify which application connected to the local proxy port, but it cannot determine the final route result. Those rows are shown as `unknown`.

## Traffic Counters

V1.1 adds ETW-based byte counters. In Normal Proxy mode the traffic scope is intentionally narrow:

- counted: application process traffic to the local v2rayN proxy port
- not counted: v2rayN core outbound traffic to the remote proxy node
- not counted: UDP, payload contents, or packet capture

In TUN mode, FlowLens asks ETW to observe TCP flows more broadly so TUN candidates can be correlated with route logs. This is still TCP metadata only, not packet capture.

ETW collection requires running FlowLens as administrator. V1.6 requests administrator permission on startup so byte counters are less likely to silently degrade. If ETW still cannot start, the UI shows `Needs administrator` or `Unavailable: <reason>`, while process attribution and proxy/direct log matching continue to work.

## Settings and Diagnostics

FlowLens stores non-sensitive UI settings in:

```text
%LocalAppData%\V2rayN.FlowLens\settings.json
```

Saved fields are the selected log or v2rayN path, proxy ports, refresh interval, attribution mode, core-process hiding, and proxy-only filtering. FlowLens does not store log contents, domain history, subscriptions, nodes, accounts, or credentials.

V1.2 adds read-only v2rayN config discovery. It checks `guiConfigs\guiNConfig.json` and currently reads the real v2rayN field `Inbound.LocalPort` as an additional local proxy-port candidate. User-entered ports are preserved; discovered ports are appended instead of replacing manual settings.

The Diagnostics tab is the first place to check when attribution looks wrong:

- `Admin`: whether ETW byte counting can run
- `ETW`: running or unavailable reason
- `Access log`: whether access logs were found
- `Log health`: whether route records were parsed
- `v2rayN config`: config discovery result
- `Proxy ports`: effective ports currently used
- `Active logs`: actual files being read
- `Match stats`: `Matched`, `PortOnly`, `LogOnly`, and `Unknown` counts
- `Attribution mode`: `NormalProxy` or `Tun`
- `TUN evidence`: TUN matching window, candidate count, and route evidence count
- `Confidence`: `Matched`, `Probable`, `Ambiguous`, and `Unknown` counts

## TUN Mode

V2 TUN mode is opt-in from the mode selector. Normal Proxy remains the default for old settings and existing users.

TUN confidence values:

- `Matched`: a process TCP candidate and route log matched target IP and port within the +/-5 second window
- `Probable`: a domain route log could not be mapped to an IP, but exactly one process candidate matched by time window and port
- `Ambiguous`: multiple process candidates matched the same evidence, so FlowLens refuses to pick one
- `Unknown`: no reliable candidate or route evidence was available

`Ambiguous` and `Unknown` rows may appear in Live Connections as diagnostic evidence, but they are not counted as confirmed application traffic in Applications, Session, Today, or History summaries. This is deliberate: TUN attribution should be conservative rather than pretending uncertain matches are exact.

## Session Statistics

V1.4 adds in-memory "this run" statistics. Session totals start when FlowLens starts or when `Reset Session` is clicked.

Session statistics:

- accumulate application and domain traffic across refreshes
- count only rows with process context, not `LogOnly` diagnostic rows
- use positive byte deltas so the same live connection is not counted repeatedly on every refresh
- stay in memory only and disappear when FlowLens exits

The session traffic scope is the same as the live counters: application traffic to the local v2rayN proxy entry, split by `proxy`, `direct`, and `unknown` route when attribution evidence exists.

V1.4.1 adds manual Session CSV export:

- `Export Applications CSV`
- `Export Domains CSV`

CSV files are written only when the user chooses a path in the save dialog. They are UTF-8 with BOM for Excel compatibility, and byte columns are raw integer bytes rather than formatted `KB` / `MB` text. Export is not a history database and does not automatically save future traffic.

## Today Statistics

V1.5 adds local "today" statistics. Today totals use the same traffic scope as Session: bytes observed on the application-to-local-v2rayN-proxy leg, split by `proxy`, `direct`, and `unknown` when route evidence exists.

Today statistics:

- persist one aggregate JSON file per day under `%LocalAppData%\V2rayN.FlowLens\history\yyyy-MM-dd.json`
- save only aggregated Applications and Domains summaries
- do not save raw connections, full access logs, subscriptions, nodes, accounts, or credentials
- exclude `LogOnly` rows and rows without process evidence
- are not cleared by `Reset Session`

If Today history cannot be loaded or saved, FlowLens keeps Live and Session views working and reports the Today history status in Diagnostics.

V1.6 adds a History tab for those same daily aggregate files:

- list local `yyyy-MM-dd.json` files under the history directory
- inspect Applications and Domains for a selected day
- export Today or selected-day Applications / Domains CSV
- open the history folder from the UI or tray menu

History remains file-based aggregate storage. FlowLens does not use SQLite, does not store raw connection rows, and does not create week/month summary views in V1.6.

## Filtering And Diagnostics

The top filter box and outbound selector filter visible tables only. They do not change Live attribution, Session accumulation, Today persistence, or History files.

V1.6 also adds copyable diagnostics from the UI and tray menu. The diagnostic report includes admin state, ETW status, active logs, proxy ports, match stats, refresh state, tray mode, session start time, and Today history path/status.

## Accuracy Limits

FlowLens does not guess when evidence is missing. If a TCP connection to the local proxy port cannot be matched to a route log by source port, the outbound and target are shown as `unknown`.

Connection snapshots are cached for a short window so route logs can still be matched after a short-lived TCP connection disappears. Status values mean:

- `Matched`: process connection and route log were matched by source port
- `PortOnly`: an app connected to the local proxy port, but no route log matched yet
- `LogOnly`: a route log exists, but the original process connection was not seen
- `Unknown`: evidence is insufficient

Byte counters are useful for finding traffic-heavy applications, but they are not expected to match carrier billing or Windows Data Usage exactly.

TUN mode is approximate. Encrypted DNS, shared CDN IPs, many simultaneous browser/game connections, and missing route logs can all reduce confidence. Treat TUN results as a way to find likely traffic-heavy applications, not as billing-grade accounting.

## Build and Test

Requirements:

- Windows
- .NET 8 SDK

Commands:

```powershell
dotnet build
dotnet test
```

Release publish command for a framework-dependent win-x64 build:

```powershell
dotnet publish .\V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj -c Release -r win-x64 --self-contained false
```

Run the app. The WPF executable requests administrator permission because ETW byte counters need elevation:

```powershell
dotnet run --project .\V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj
```

## Manual MVP Check

1. Start v2rayN with TUN disabled.
2. Set FlowLens proxy ports to match v2rayN local ports, for example `10808,10809`, or let config discovery append `Inbound.LocalPort`.
3. Enable v2rayN Core logs at `info` level and restart the v2rayN core.
4. Enter a v2rayN core log file or log directory.
5. Use a browser to access sites such as `google.com` or `github.com`.
6. Confirm the app shows the browser process instead of attributing everything to `xray.exe`, `sing-box.exe`, or `HttpProxy.exe`.
7. Confirm the health warning disappears when route logs are present and `proxy` / `direct` values appear in the live connection table.
8. If running as administrator, confirm application and domain rows show non-zero traffic after browsing.
9. Confirm the Session tab keeps accumulated totals after live connections disappear.
10. Click `Reset Session` and confirm session totals clear, then grow again after more browsing.
11. Confirm Today totals survive restart and History can read the selected day.
12. Export Today or History CSV and confirm text is readable and byte columns are raw numbers.

## Manual TUN Check

1. Switch FlowLens mode to `Tun`.
2. Enable v2rayN TUN mode and keep Core logs at `info`.
3. Visit `google.com`, `github.com`, and a direct site such as `baidu.com`.
4. Confirm Live Connections shows TUN rows with `Matched`, `Probable`, `Ambiguous`, or `Unknown` confidence.
5. Start two applications accessing the same site at the same time and confirm FlowLens shows `Ambiguous` or another conservative result instead of forcing one process.
6. Disable Core logs and confirm TUN mode still shows TCP/ETW observations, but outbound route evidence becomes `unknown`.

## Related Projects / References

FlowLens is not trying to replace general network monitors or firewalls. Its specific value is combining v2rayN/Xray access logs with Windows TCP/ETW data so local proxy connections can be attributed back to the original application and final `proxy` / `direct` route.

Reference projects reviewed for future direction:

- [OpenNetMeter](https://github.com/Ashfaaq18/OpenNetMeter): useful reference for Windows session/today/history traffic views and application data-usage presentation.
- [WhoYouCalling](https://github.com/H4NM/WhoYouCalling): useful reference for ETW TCP/IP and DNS-based process network activity enrichment.
- [Sniffnet](https://github.com/GyulyVGC/sniffnet): useful reference for filtering, notifications, and mature network-monitor UX.
- [Portmaster](https://github.com/safing/portmaster) and [simplewall](https://github.com/henrypp/simplewall): useful product and WFP/firewall references, but GPL-3.0 source code is not copied into FlowLens.

See `docs/reference-analysis.md` for the current reference audit and V1.4 recommendation.

## Future Work

Future work can improve TUN accuracy with better DNS correlation or additional Windows networking evidence, but should keep the same conservative policy: show `Unknown` or `Ambiguous` rather than pretending uncertain matches are exact.

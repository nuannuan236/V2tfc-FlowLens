# v2rayN FlowLens

v2rayN FlowLens is a Windows desktop prototype for v2rayN proxy traffic attribution.

It is a read-only monitoring tool. It does not replace v2rayN, edit v2rayN configuration, manage subscriptions or nodes, capture packets, or upload logs/domains to a remote service.

## MVP Scope

The current MVP targets normal non-TUN system proxy mode:

- Read v2rayN/Xray/sing-box log files.
- Read current Windows TCP connections with PID and process name.
- Detect applications connected to configured local proxy ports, such as `10808` and `10809`.
- Match the application's local ephemeral source port to log records like `from 127.0.0.1:<port>`.
- Show application ranking, live attributed connections, domain ranking, and ETW-based byte counters.
- Show this-run session totals for applications and domains.
- Show diagnostics for admin status, ETW status, access log discovery, v2rayN config discovery, active logs, proxy ports, and match status counts.

TUN mode attribution is not implemented in this version.

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

V1.1 adds ETW-based byte counters for normal non-TUN proxy mode. The traffic scope is intentionally narrow:

- counted: application process traffic to the local v2rayN proxy port
- not counted: v2rayN core outbound traffic to the remote proxy node
- not counted: TUN traffic, UDP, payload contents, or packet capture

ETW collection requires running FlowLens as administrator. If ETW cannot start, the UI shows `ETW traffic unavailable`, while process attribution and proxy/direct log matching continue to work.

## Settings and Diagnostics

FlowLens stores non-sensitive UI settings in:

```text
%LocalAppData%\V2rayN.FlowLens\settings.json
```

Saved fields are the selected log or v2rayN path, proxy ports, refresh interval, core-process hiding, and proxy-only filtering. FlowLens does not store log contents, domain history, subscriptions, nodes, accounts, or credentials.

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

## Session Statistics

V1.4 adds in-memory "this run" statistics. Session totals start when FlowLens starts or when `Reset Session` is clicked.

Session statistics:

- accumulate application and domain traffic across refreshes
- count only rows with process context, not `LogOnly` diagnostic rows
- use positive byte deltas so the same live connection is not counted repeatedly on every refresh
- stay in memory only and disappear when FlowLens exits

The session traffic scope is the same as the live counters: application traffic to the local v2rayN proxy entry, split by `proxy`, `direct`, and `unknown` route when attribution evidence exists.

## Accuracy Limits

FlowLens does not guess when evidence is missing. If a TCP connection to the local proxy port cannot be matched to a route log by source port, the outbound and target are shown as `unknown`.

Connection snapshots are cached for a short window so route logs can still be matched after a short-lived TCP connection disappears. Status values mean:

- `Matched`: process connection and route log were matched by source port
- `PortOnly`: an app connected to the local proxy port, but no route log matched yet
- `LogOnly`: a route log exists, but the original process connection was not seen
- `Unknown`: evidence is insufficient

Byte counters are useful for finding traffic-heavy applications, but they are not expected to match carrier billing or Windows Data Usage exactly.

## Build and Test

Requirements:

- Windows
- .NET 8 SDK

Commands:

```powershell
dotnet build
dotnet test
```

Run the app. Use an elevated terminal if you want ETW traffic counters:

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

## Related Projects / References

FlowLens is not trying to replace general network monitors or firewalls. Its specific value is combining v2rayN/Xray access logs with Windows TCP/ETW data so local proxy connections can be attributed back to the original application and final `proxy` / `direct` route.

Reference projects reviewed for future direction:

- [OpenNetMeter](https://github.com/Ashfaaq18/OpenNetMeter): useful reference for Windows session/today/history traffic views and application data-usage presentation.
- [WhoYouCalling](https://github.com/H4NM/WhoYouCalling): useful reference for ETW TCP/IP and DNS-based process network activity enrichment.
- [Sniffnet](https://github.com/GyulyVGC/sniffnet): useful reference for filtering, notifications, and mature network-monitor UX.
- [Portmaster](https://github.com/safing/portmaster) and [simplewall](https://github.com/henrypp/simplewall): useful product and WFP/firewall references, but GPL-3.0 source code is not copied into FlowLens.

See `docs/reference-analysis.md` for the current reference audit and V1.4 recommendation.

## Future TUN Work

TUN attribution should be treated as V2 work. It will need approximate matching across original process connection records, core routing logs, time windows, destination IP/port, and domain data. It should keep the same policy as MVP: show `unknown` rather than pretending uncertain matches are exact.

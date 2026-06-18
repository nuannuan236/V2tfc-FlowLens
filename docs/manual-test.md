# Manual Test - 2026-06-13

## V2.0.3 TUN Evidence Acquisition - 2026-06-18

### Scope

Run a stronger TUN validation by temporarily clearing the Windows system proxy while keeping v2rayN TUN enabled. The purpose was to remove the NormalProxy path from the test and check whether FlowLens can get `Matched` / `Probable` evidence from true TUN candidates.

This pass did not change v2rayN JSON files and did not add code.

### Code Baseline

- Repository state before the run: clean on `main`
- `dotnet build .\V2rayN.FlowLens.sln --no-restore`: passed, 0 warnings, 0 errors
- `dotnet test .\V2rayN.FlowLens.sln --no-restore`: passed, 84 tests

Note: the first build attempt failed because a running FlowLens process locked `V2rayN.FlowLens.Core.dll` in the Debug app output folder. After stopping FlowLens, sequential build and test passed.

### Environment

- v2rayN root: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained`
- Access log: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs\Vaccess_2026-06-18.txt`
- v2rayN TUN: enabled
  - `TunModeItem.EnableTun = true`
- v2rayN system proxy mode after user action:
  - `SystemProxyType = 0`
- Windows system proxy after user action:
  - `ProxyEnable = 0`
  - `ProxyServer = 127.0.0.1:10808` remained stored but inactive
- IPv4 default route included `singbox_tun`:
  - `0.0.0.0/0 -> 172.18.0.2`
- FlowLens settings:
  - `AttributionMode = Tun`
  - `OnlyShowProxy = false`
- FlowLens process: PID `7676`, administrator window running

The user noted that their normal daily setup is TUN enabled plus automatic system proxy. This test intentionally disabled system proxy only to isolate TUN evidence.

### Traffic Sources

Traffic was generated with system proxy disabled:

- `curl.exe --noproxy "*"` against:
  - `https://www.google.com`
  - `https://github.com`
  - `https://www.baidu.com`
- Slow `curl.exe --noproxy "*"` download against `https://github.com` with `--limit-rate 1k`
- A separate Edge instance with:
  - `--user-data-dir=%TEMP%\flowlens-tun-v203\edge-profile`
  - `--no-proxy-server`
  - `https://github.com`
  - `https://www.google.com`
  - `https://www.baidu.com`

All quick curl requests returned HTTP 200, so TUN connectivity worked with system proxy disabled.

### TCP Evidence

With system proxy disabled, the TCP table showed original non-core applications connecting directly from the TUN-side local address `172.18.0.1` to non-loopback remotes:

- `curl.exe` PID `18452` -> `140.82.116.4:443`
- `curl.exe` PID `19200` -> `140.82.116.4:443`
- `msedge.exe` PID `7760` -> `140.82.116.3:443`
- `msedge.exe` PID `7760` -> `185.199.108.215:443`
- `msedge.exe` PID `7760` -> Google IPs on `:443`

This is stronger than the V2 desktop validation because original app TUN candidates were visible in Windows TCP state.

Local proxy check during the same run still showed many `127.0.0.1 -> 127.0.0.1:10808` rows, but these were owned by `sing-box.exe` or `Idle.exe`, not the tested `curl.exe` / `msedge.exe` clients. That indicates the system proxy path was no longer the direct browser/client path, though v2rayN/sing-box still used local SOCKS internally.

### Access Log Evidence

The access log grew during the run:

- before: `815505` bytes
- after: `829916` bytes

Even with Windows system proxy disabled, the fresh access log rows still used `socks` inbound:

- `tcp:142.251.40.110:443 [socks -> proxy]`
- `tcp:140.82.116.3:443 [socks >> proxy]`
- `tcp:185.199.108.215:443 [socks >> proxy]`
- `tcp:140.82.116.4:443 [socks >> proxy]`

No `tun -> proxy/direct` access-log inbound was observed in this run. The practical interpretation is that v2rayN/sing-box TUN traffic can still surface in the Xray access log as local SOCKS route evidence, not as a distinct `tun` inbound label.

### Core TUN Diagnostic

A temporary console diagnostic outside the repository referenced the current `V2rayN.FlowLens.Core` project and used:

- `WindowsTcpConnectionReader`
- `LogFileReader`
- `TunAttributionEngine`

First diagnostic run:

- TCP rows: `7107`
- Non-loopback rows: `3749`
- Parsed logs: `5000`
- Confidence:
  - `Ambiguous`: 1
  - `Unknown`: 512
  - `Matched`: 0
  - `Probable`: 0

Short-window slow-curl diagnostic:

- Confidence:
  - `Ambiguous`: 1
  - `Unknown`: 534
  - `Matched`: 0
  - `Probable`: 0
- Ambiguous row:
  - target: `140.82.116.4:443`
  - outbound: `proxy`
  - evidence: `Multiple processes matched the same target IP and port within +/-5 seconds.`
  - candidates included `curl.exe(19200)` and `Idle.exe(0)`

Proxy-only checks:

- `ProxyOnlyRows = 160`
- `NonProxyRowsAfterProxyOnly = 0`
- `MissingLogProxyOnlyRows = 0`

This confirms the V2.0.2 missing-log proxy-only behavior still holds under the stronger TUN evidence run.

### Verdict

V2.0.3 TUN evidence acquisition: partial pass.

Pass:

- System proxy was disabled.
- TUN remained enabled and the `singbox_tun` route was active.
- Test traffic still reached the network.
- Windows TCP state exposed original app TUN candidates such as `curl.exe` and `msedge.exe`.
- FlowLens/Core produced conservative `Ambiguous` / `Unknown` instead of forcing a false application attribution.
- `OnlyShowProxy` continued to filter non-proxy and missing-log unknown rows.

Not passed / not confirmed:

- No `Matched` or `Probable` TUN attribution was observed.
- No confirmed application-level TUN attribution entered the certain statistics path.
- v2rayN access logs still exposed route evidence as `[socks -> ...]`, not `[tun -> ...]`.
- The `curl.exe` GitHub case became `Ambiguous` because another candidate (`Idle.exe(0)`) shared the same target IP and port within the match window.

Conclusion:

`TUN fallback/conservative behavior: pass`

`True TUN application attribution: not confirmed`

If the next step is V2.1, it should be a diagnostic/evidence export feature, not a more aggressive guessing algorithm. Useful next evidence would include a per-refresh export of candidates, route evidence, match-window decisions, consumed candidates, and rejected candidates.

## V2 TUN Desktop Validation - 2026-06-18

### Scope

Validate V2 TUN behavior on the real desktop after freezing V2.0.2:

- NormalProxy regression check
- v2rayN TUN enabled state
- FlowLens `Tun` mode refresh with ETW
- conservative TUN attribution behavior
- `Proxy only` missing-log filtering through the Core attribution path

This pass does not claim billing-grade TUN accounting.

### Code Baseline

- Frozen commit: `f5a2b59 Fix TUN proxy-only missing-log filtering`
- `dotnet build .\V2rayN.FlowLens.sln --no-restore`: passed, 0 warnings, 0 errors
- `dotnet test .\V2rayN.FlowLens.sln --no-restore`: passed, 84 tests

Note: an earlier parallel build/test attempt hit an `obj` write lock while tests were also building the Core project. Sequential build and test passed.

### Environment

- v2rayN root: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained`
- Access log: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs\Vaccess_2026-06-18.txt`
- Local proxy ports: `10808,10809`
- System proxy during the run: `127.0.0.1:10808`
- FlowLens was run as administrator.
- FlowLens was switched to `Tun` by updating its local settings and restarting because the persisted settings still showed `NormalProxy` after the user reported switching.
- FlowLens process after restart: PID `2592`
- ETW: `V2rayN.FlowLens.Traffic.2592` running

### NormalProxy Baseline

Before enabling TUN:

- `TunModeItem.EnableTun = false`
- v2rayN system proxy was enabled.
- Browser traffic was generated for:
  - `https://www.google.com`
  - `https://github.com`
  - `https://www.baidu.com`

Observed original applications connecting to `127.0.0.1:10808`:

- `msedge.exe`
- `twinkstar.exe`
- `Codex.exe`

Observed route evidence:

- `www.google.com:443 [socks -> proxy]`
- `github.com:443 [socks >> proxy]`
- `github.githubassets.com:443 [socks >> proxy]`
- `www.baidu.com:443 [socks -> direct]`
- `hectorstatic.baidu.com:443 [socks -> direct]`
- `mbd.baidu.com:443 [socks -> direct]`

Today history grew and showed non-zero application/domain byte totals, including `msedge.exe` proxy/direct split.

Result: NormalProxy regression check passed.

### TUN Run

After the user enabled TUN:

- `TunModeItem.EnableTun = true`
- IPv4 default route included `singbox_tun`:
  - `0.0.0.0/0 -> 172.18.0.2`
- FlowLens local settings showed:
  - `AttributionMode = Tun`
  - `OnlyShowProxy = false`
- Browser and `curl.exe --noproxy "*"` traffic were generated for:
  - `https://www.google.com`
  - `https://github.com`
  - `https://www.baidu.com`

Important observation:

- The latest access log records still used `socks` inbound, not `tun` inbound.
- No `[(tun) -> ...]` access log records were found in `Vaccess_2026-06-18.txt`.
- Recent route summary from the access log showed:
  - `socks -> proxy`: 251
  - `socks -> direct`: 49

Examples after TUN was enabled:

- `www.google.com:443 [socks -> proxy]`
- `github.com:443 [socks >> proxy]`
- `github.githubassets.com:443 [socks >> proxy]`
- `www.baidu.com:443 [socks -> direct]`
- `tcp:142.251.155.119:443 [socks -> proxy]`
- `tcp:140.82.116.4:443 [socks >> proxy]`

Non-loopback TCP rows existed, but many were owned by `sing-box.exe` and `xray.exe`; browser traffic still appeared to be using the local SOCKS/system proxy path in this desktop configuration.

### Direct Core Diagnostic

Because Computer Use could not capture/control the elevated WPF window, a temporary diagnostic console app was run outside the repository. It referenced the current `V2rayN.FlowLens.Core` project and used the same `WindowsTcpConnectionReader`, `LogFileReader`, and `TunAttributionEngine`.

Diagnostic input:

- TCP rows: `7925`
- Non-loopback TCP rows: `4018`
- Parsed logs: `5000`
- Active access log: `Vaccess_2026-06-18.txt`

Observed TUN confidence distribution in one run:

- `Ambiguous`: 1
- `Unknown`: 132
- `Matched`: 0
- `Probable`: 0

Observed behavior:

- Unknown rows included readable evidence such as `TCP candidate has no route evidence in the TUN matching window.`
- The single `Ambiguous` result was not forced into a specific application.
- A later run had only `Unknown` rows because the time window changed; this is expected for live TCP/log matching.

Proxy-only diagnostic:

- `OnlyShowProxy = true`: `ProxyOnlyRows = 31`
- Non-proxy rows after proxy-only filtering: `0`
- Missing route logs with proxy-only enabled: `MissingLogProxyOnlyRows = 0`

Result: V2.0.2's proxy-only missing-log fix is confirmed through the Core path.

### Diagnostics / UI Automation Note

Computer Use failed to initialize with the same `@oai/sky` package export error seen in earlier runs, so WPF visual inspection of the elevated window was not available from Codex.

Confirmed through non-UI evidence:

- FlowLens administrator process running
- ETW session running
- FlowLens settings set to `Tun`
- v2rayN TUN enabled
- `singbox_tun` route present
- access log and TCP table were readable
- Core TUN attribution returned conservative `Unknown` / `Ambiguous` rather than guessing

Not visually confirmed in WPF:

- Live Connections tab rendering of `Mode = Tun`
- Diagnostics tab text
- on-screen `Confidence` and `Evidence` columns

### Verdict

V2 TUN conservative attribution: partial pass.

Pass:

- NormalProxy default path did not regress.
- v2rayN TUN was enabled and `singbox_tun` route was present.
- FlowLens could run in `Tun` mode with ETW active.
- Core TUN attribution remained conservative.
- `Ambiguous` / `Unknown` were not converted into forced application attribution.
- `OnlyShowProxy` removed non-proxy rows, including missing-log unknown rows.

Not confirmed / not passed in this desktop run:

- No `Matched` or `Probable` TUN attribution was observed.
- Original browser application attribution in true TUN mode was not confirmed.
- Access logs still showed `socks` inbound after TUN was enabled, likely because system proxy remained active and browser traffic continued through `127.0.0.1:10808`.
- WPF Diagnostics and Live Connections visual verification could not be captured due the Computer Use plugin failure.

Conclusion:

`V2 TUN conservative attribution: partial pass`

Scope: approximate attribution only, not billing-grade accounting.

Remaining limits:

- UDP/DNS ETW and exact TUN packet attribution are not implemented.
- This environment did not produce enough non-core, non-loopback, route-matched evidence to validate `Matched` / `Probable` TUN application attribution.
- A stronger next TUN test should temporarily avoid browser system-proxy routing or use an app that does not use `127.0.0.1:10808`, then verify whether v2rayN emits route evidence that can be correlated to that app.

## V2 TUN Attribution - 2026-06-18

### V2.0.1 Closure

V2.0.1 fixes two validation risks found during review:

- one TUN TCP candidate can only be consumed once per refresh, so repeated route log evidence does not double-count Live/Application/Session/Today traffic
- TUN mode now honors `Proxy only` and hides `direct` / `unknown` rows when enabled

Additional unit tests cover exact IP duplicate evidence, domain probable duplicate evidence, Proxy-only filtering, and non-Proxy-only direct/unknown visibility.

### Scope

Validate the first TUN attribution implementation:

- Normal Proxy remains the default mode.
- TUN mode is opt-in.
- No packet capture, WinDivert, Npcap, WFP firewall control, or v2rayN config writes.
- TUN confidence is conservative: `Matched`, `Probable`, `Ambiguous`, or `Unknown`.

### Automated Validation

Unit tests cover:

- legacy settings without `AttributionMode` load as `NormalProxy`
- TUN exact target IP/port unique candidate returns `Matched`
- TUN domain route evidence with one time/port candidate returns `Probable`
- multiple candidates for the same evidence return `Ambiguous`
- missing TCP candidates or missing logs return `Unknown`
- Session and Today accumulators exclude `Ambiguous` and `Unknown` from confirmed application summaries
- existing Normal Proxy source-port attribution tests still pass

### Required Manual Run

This pass does not claim real TUN desktop validation. Run these steps:

1. Start FlowLens as administrator.
2. Confirm default mode is `NormalProxy`.
3. With v2rayN TUN disabled, repeat a quick Normal Proxy test and confirm source-port `proxy/direct` matching still works.
4. Switch FlowLens mode to `Tun`.
5. Enable v2rayN TUN mode and keep Core logs at `info`.
6. Visit:
   - `https://www.google.com`
   - `https://github.com`
   - one direct site, for example `https://www.baidu.com`
7. Confirm Live Connections shows TUN rows with Mode `Tun`, confidence values, target, outbound, and evidence text.
8. Start two applications visiting the same site at the same time and confirm FlowLens reports `Ambiguous` or another conservative result instead of forcing one application.
9. Disable Core logs and confirm TUN mode still shows TCP/ETW observations, but route/outbound evidence falls back to `unknown`.
10. Open Diagnostics and confirm Attribution mode, TUN evidence, and Confidence stats are visible.

### Current Limits

- TUN mode is approximate and may produce `Probable`, `Ambiguous`, or `Unknown` frequently.
- Domain route evidence is not mapped through DNS in V2; it is only correlated by time window and port.
- UDP and DNS ETW are not implemented.
- TUN results are useful for finding likely large traffic sources, not for billing-grade totals.

## V1.6 Pre-TUN Closeout - 2026-06-16

### Scope

Validate the final non-TUN usability pass before any TUN attribution work:

- Today real-run behavior and local daily history
- History tab for local aggregate JSON files
- Today and History CSV export
- administrator startup / ETW status clarity
- filter controls that only change visible rows
- copyable diagnostics and tray history actions

Still out of scope:

- TUN attribution
- packet capture
- writing v2rayN configuration
- SQLite or query database
- week/month charts or anomaly alerts

### Automated Validation

Code validation for this pass should include:

- `dotnet build .\V2rayN.FlowLens.sln --no-restore`
- `dotnet test .\V2rayN.FlowLens.Tests\V2rayN.FlowLens.Tests.csproj --no-restore`

Unit tests cover:

- history browser date listing and selected-day load
- damaged history JSON safe failure
- Today / History CSV behavior through the shared exporter
- keyword and outbound filtering without mutating source collections
- diagnostic report text containing admin, ETW, log, ports, match stats, and history status

### Required Manual Run

This section is a checklist until a desktop administrator run is completed:

1. Start FlowLens and accept the administrator prompt.
2. Keep v2rayN in normal system proxy mode with TUN disabled.
3. Browse `google.com`, `github.com`, and a direct site such as `baidu.com`.
4. Confirm ETW shows `Running` and Live rows show original applications with non-zero bytes.
5. Confirm Today Applications / Domains grow.
6. Click `Reset Session` and confirm Session clears while Today and History remain.
7. Exit and restart FlowLens, then confirm Today reloads from the current day JSON file.
8. Open the History tab and confirm today appears in the date list and shows aggregate data.
9. Export Today Applications / Domains CSV and selected-day History Applications / Domains CSV.
10. Open the exported CSV files and confirm text is readable and bytes are raw integers.
11. Use the keyword filter and outbound selector; confirm only visible rows change.
12. Use `Open History Folder` from the UI or tray menu.
13. Use `Copy Diagnostics` from the UI or tray menu and confirm the clipboard text contains ETW, logs, ports, match stats, and Today history.

### Current Limits

- History is daily aggregate JSON only; there is still no week/month query UI.
- Filters are display-only and do not alter accumulated or persisted data.
- Today / History CSV export is manual and one-shot.
- V1.6 requests administrator permission, but ETW can still be unavailable if Windows denies the ETW session.

## V1.5 Today Statistics - 2026-06-16

### Scope

Validate Today statistics and local aggregate history:

- non-TUN normal system proxy mode only
- no v2rayN config writes
- no packet capture
- no database
- no raw connection or full log persistence

### Automated Validation

Unit tests cover:

- same connection refreshes do not double count Today bytes
- positive byte deltas accumulate
- proxy/direct/unknown byte splits
- `LogOnly` exclusion
- loading from persisted summaries
- day-file read/write round trip
- missing, empty, and damaged history files
- separate file paths for separate days

### Required Manual Run

This pass does not claim a completed administrator browser run. Run these steps on the desktop:

1. Start FlowLens as administrator.
2. Keep v2rayN in normal system proxy mode with TUN disabled.
3. Confirm the active local proxy port, expected `127.0.0.1:10808` unless v2rayN config changed.
4. Visit:
   - `https://www.google.com`
   - `https://github.com`
   - one direct site, for example `https://www.baidu.com`
5. Confirm Today Applications and Today Domains grow.
6. Click `Reset Session` and confirm Session clears while Today does not clear.
7. Exit and restart FlowLens, then confirm Today reloads from the current day JSON file.
8. Open Diagnostics and confirm Today history shows the current date, file path, and load/save status.
9. Inspect `%LocalAppData%\V2rayN.FlowLens\history\yyyy-MM-dd.json` and confirm it contains only aggregate Applications / Domains summaries, not raw connection rows or full logs.

### Current Limits

- Today statistics are daily aggregate files only; there is no query UI for prior days.
- Today CSV export is not included in V1.5; Session CSV remains unchanged.
- If FlowLens restarts while a long-lived connection is still active, connection count can be slightly duplicated, but bytes still reflect FlowLens-observed ETW deltas.
- Today totals are for traffic-heavy attribution, not billing-grade accounting.

## V1.4.2 Administrator Validation - 2026-06-16

### Scope

Validate the V1.4.1 runtime path on the real desktop without adding new features:

- elevated FlowLens run
- non-TUN v2rayN system proxy mode
- ETW traffic counters
- Live Connections source-port attribution
- Session Applications / Domains accumulation
- Session CSV export

Out of scope for this validation:

- TUN attribution
- database or long-term history
- today/week/month statistics
- DNS ETW enrichment
- new attribution algorithms

### Environment

- FlowLens: `V2rayN.FlowLens.App\bin\Debug\net8.0-windows\V2rayN.FlowLens.App.exe`
- FlowLens mode: administrator / elevated
- ETW: running
  - `logman query -ets` showed `V2rayN.FlowLens.Traffic.17964` as `Running`
- v2rayN root: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained`
- v2rayN mode: normal system proxy mode
- TUN: disabled
  - Confirmed from `guiConfigs\guiNConfig.json`: `TunModeItem.EnableTun = false`
- Local proxy port:
  - v2rayN inbound `LocalPort = 10808`
  - Windows system proxy `ProxyServer = 127.0.0.1:10808`
- Active access log:
  - `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs\Vaccess_2026-06-16.txt`

### Test Sites

Opened during the validation:

- `https://www.google.com`
- `https://github.com`
- `https://www.baidu.com`

### Route Evidence

Access log records observed after browsing:

- `play.google.com:443 [socks -> proxy]`
- `github.githubassets.com:443 [socks >> proxy]`
- `github.com:443 [socks >> proxy]`
- `ogs.google.com:443 [socks -> proxy]`
- `www.baidu.com:443 [socks -> direct]`
- `hectorstatic.baidu.com:443 [socks -> direct]`
- `mbd.baidu.com:443 [socks -> direct]`
- `sp1.baidu.com:443 [socks -> direct]`

Result:

- `google.com` / `github.com`: proxy route observed.
- `baidu.com`: direct route observed.

### Application Attribution

Windows TCP rows confirmed original applications connecting to `127.0.0.1:10808`, including:

- `msedge.exe` PID `3108`
- `twinkstar.exe` PID `16292`
- `codex.exe` PID `19608`

Applications view confirmed non-core attribution and route split:

- `msedge.exe` showed non-zero traffic with both proxy and direct counts.
- `twinkstar.exe` showed direct traffic.
- The traffic was not collapsed into only `xray.exe`, `sing-box.exe`, or `HttpProxy.exe`.

### Live Connections

User-provided Live Connections screenshot confirmed:

- rows are `Matched`
- `Outbound` shows `proxy` and `direct`
- original applications include `codex.exe` and `twinkstar.exe`
- `Sent`, `Received`, and `Total` can all be non-zero

Examples visible in the screenshot:

- `codex.exe` PID `19608`, source port `7300`, `socks -> proxy`, `Sent 2.64 KB`, `Received 2.64 KB`, `Total 5.28 KB`, `Matched`
- `codex.exe` PID `19608`, source port `7293`, `socks -> proxy`, `Sent 3.1 KB`, `Received 3.1 KB`, `Total 6.2 KB`, `Matched`
- `twinkstar.exe` PID `16292`, source port `7296`, `socks -> direct`, `Sent 4.13 KB`, `Received 4.13 KB`, `Total 8.25 KB`, `Matched`

This confirms V1.2.1's receive-byte fix works on this machine during the V1.4.2 validation run.

### Session Statistics

User-provided Session screenshot confirmed Session Applications and Session Domains retain accumulated totals:

Applications examples:

- `codex.exe`: `85.03 MB` total, proxy traffic present
- `msedge.exe`: `587.49 KB` total, proxy and direct split present
- `twinkstar.exe`: `83.17 KB` total, direct traffic present

Domain examples:

- `chatgpt.com`: `84.14 MB`
- `www.google.com`: `236.05 KB`
- `play.google.com`: `31.07 KB`
- `hector.baidu.com`: direct domain traffic present
- `www.doubao.com`: direct domain traffic present

Result:

- Session Applications: pass.
- Session Domains: pass.
- Session totals continue to exist independently of the current visible Live rows.

### Reset Session

Not marked as confirmed in this record.

The V1.4.2 screenshots confirm Session accumulation, but they do not directly show `Reset Session` clearing Session Applications / Domains and then accumulating again after new browsing. This remains a small manual check unless separately confirmed.

### CSV Export

User confirmed both CSV exports are normal:

- Session Applications CSV: pass
- Session Domains CSV: pass
- Text encoding: no mojibake reported
- CSV fields: accepted by manual check
- byte columns: accepted by manual check

The current exporter writes UTF-8 with BOM and raw numeric byte columns, matching the intended V1.4.1 behavior.

### Automation Note

Computer Use was not usable for this elevated WPF window in this run. The plugin failed to initialize with an `@oai/sky` package export error, and the elevated FlowLens window could not be reliably controlled from the non-elevated Codex process. Validation therefore used:

- shell-level process / ETW / log / TCP checks
- passive screenshots
- user-provided UI screenshots
- user-confirmed CSV checks

This is an automation limitation, not a FlowLens runtime failure.

### V1.4.2 Verdict

Pass with one small unconfirmed item:

- Pass: administrator run
- Pass: ETW running
- Pass: non-TUN v2rayN system proxy mode
- Pass: access log route parsing for proxy and direct
- Pass: original application attribution
- Pass: Live Connections `Sent` / `Received` / `Total`
- Pass: Session Applications and Domains accumulation
- Pass: Applications CSV export
- Pass: Domains CSV export
- Not confirmed in screenshots: `Reset Session` clear-and-regrow behavior

## V1.4.1 Closure Notes - 2026-06-16

### Scope

Close V1.4 into a small deliverable version:

- Keep V1.4 in-memory Session statistics.
- Add manual CSV export for Session Applications and Session Domains.
- Do not add TUN attribution, database storage, DNS ETW, long-term history, or new attribution rules.

### Code Validation

- Added Session CSV export buttons:
  - `Export Applications CSV`
  - `Export Domains CSV`
- CSV export is manual and one-shot through a Windows save dialog.
- CSV files are written as UTF-8 with BOM for Excel compatibility.
- Byte columns use raw integer bytes, not formatted `KB` / `MB` strings.
- Empty Session lists export a header-only CSV.
- CSV export errors are reported in the FlowLens status bar instead of crashing the app.

### Required Administrator Manual Run

This Codex pass did not complete the elevated UI/browser manual run because it requires interactive administrator approval and real browser activity. Do not treat this section as a pass until it is run on the desktop.

Run these steps:

1. Start FlowLens as administrator.
2. Keep v2rayN in normal system proxy mode with TUN disabled.
3. Confirm the active local proxy port, expected `127.0.0.1:10808` on the current machine unless v2rayN config changed.
4. Visit:
   - `https://www.google.com`
   - `https://github.com`
   - one direct site, for example `https://www.baidu.com`
5. Confirm Live Connections may shrink or disappear after traffic stops, while Session Applications and Session Domains retain accumulated totals.
6. Click `Reset Session` and confirm Session Applications and Session Domains clear.
7. Browse again and confirm Session totals grow from zero.
8. Export both Session CSV files and open them in Excel or a text editor.

Expected CSV checks:

- Application/domain text is readable and not mojibake.
- Numeric byte columns contain plain integers.
- Applications CSV has `Application`, `PID`, `TotalBytes`, `ProxyBytes`, `DirectBytes`, `UnknownBytes`, `ConnectionCount`, `LastActive`.
- Domains CSV has `Domain`, `TotalBytes`, `ProxyBytes`, `DirectBytes`, `UnknownBytes`, `ConnectionCount`, `Applications`, `LastActive`.
- After `Reset Session`, exporting before new browsing should produce only headers or only new post-reset statistics.

### Current Limits

- Session statistics remain in-memory only and reset on app exit or `Reset Session`.
- Session byte scope remains the application-to-local-v2rayN-proxy leg.
- `LogOnly` rows are not treated as application evidence and should not appear in Applications CSV.
- TUN attribution remains out of scope.

## V1.2.1 Closure Notes - 2026-06-15

### Scope

Close two V1.2 validation issues without starting V1.3:

- ETW receive bytes were not visually confirmed and may stay at `0 B` when ETW receive events use a PID that does not match the TCP table PID.
- `LogOnly` rows could be misread as a real PID `0` application source.

Still out of scope:

- TUN attribution.
- Long-term traffic history.
- Packet capture, drivers, or WinDivert.
- Writing v2rayN configuration.

### Code Changes

- Added receive-byte fallback in the ETW traffic accumulator:
  - send bytes still use the exact ETW PID + TCP tuple key.
  - receive bytes first try the exact key.
  - if the ETW PID does not match, receive bytes fall back to the unique active TCP tuple retained from the current FlowLens snapshot.
  - unmatched or ambiguous receive bytes are dropped instead of being assigned to PID `0`, `xray.exe`, or another guessed process.
- Changed `LogOnly` display semantics:
  - Application: `Unknown`
  - PID: empty / null
  - Status: `LogOnly`
  - Applications summary excludes `LogOnly` rows because they do not have process evidence.

### Validation

- `dotnet build .\V2rayN.FlowLens.sln --no-restore` passed: 0 warnings, 0 errors.
- `dotnet test .\V2rayN.FlowLens.Tests\V2rayN.FlowLens.Tests.csproj --no-restore` passed: 41 tests.

New tests cover:

- v2rayN `Inbound` object and array config shapes.
- ETW send exact PID attribution.
- ETW receive fallback when PID differs but the active TCP tuple is unique.
- ETW receive drop behavior when no tuple matches or multiple tuples match.
- `LogOnly` as `Unknown` with null PID.
- Applications summary excluding `LogOnly`.

### Manual Status

V1.2 already confirmed:

- Admin/elevated run works.
- ETW session can run.
- Access log discovery works.
- Source-port matching shows proxy/direct.
- Application totals and Sent/Total can grow during browser traffic.

V1.2.1 still needs one elevated UI check:

- Launch FlowLens as administrator.
- Keep v2rayN in non-TUN system proxy mode on `127.0.0.1:10808`.
- Visit `google.com`, `github.com`, and `baidu.com`.
- In Live Connections, confirm matched rows show non-zero `Sent`, non-zero `Total`, and observe whether `Received` is now non-zero.
- Confirm `LogOnly` rows do not appear as `Idle.exe` or PID `0` in Applications.

If `Received` remains `0 B` after this fix, treat it as an ETW receive attribution limitation on this machine and do not expand V1.2.1 into packet capture or ETW redesign.

## V1.2 Short Hardware Run - 2026-06-14

### Scope

Validate the V1.2 path on the real machine:

- FlowLens elevated.
- v2rayN non-TUN.
- Core access log enabled.
- ETW byte accounting running.
- Browser traffic through `google.com`, `github.com`, and `baidu.com`.

### Environment

- v2rayN: `v2rayN - V7.22.6 - X64 - 以管理员身份运行`
- v2rayN root: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained`
- Mode: normal system proxy mode
- TUN: disabled
  - Confirmed from `guiConfigs\guiNConfig.json`: `TunModeItem.EnableTun = false`
- System proxy: enabled
  - Registry `ProxyEnable = 1`
  - Registry `ProxyServer = 127.0.0.1:10808`
- FlowLens: launched elevated from `V2rayN.FlowLens.App\bin\Debug\net8.0-windows\V2rayN.FlowLens.App.exe`

### Diagnostics

- Admin: OK
  - FlowLens was launched with `Start-Process -Verb RunAs`.
  - The non-elevated shell could not read the elevated process `Path`, which is consistent with an elevated process.
- ETW: Running
  - `logman query -ets` showed `V2rayN.FlowLens.Traffic.18328` as `Running`.
- Access log: OK
  - Active access log: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs\Vaccess_2026-06-14.txt`
  - FlowLens status showed parsed logs, for example `Logs: 3018`.
- Proxy ports: OK
  - FlowLens UI showed `10808,10809`.
  - The active system proxy used `127.0.0.1:10808`.

### Issue Found And Fixed During This Run

The first elevated refresh failed with:

`The requested operation requires an element of type 'Object', but the target element has type 'Array'.`

Root cause:

- Real v2rayN 7.x stores `Inbound` in `guiNConfig.json` as an array.
- `V2rayNConfigDiscovery` only handled `Inbound` as an object.

Fix applied:

- `V2rayNConfigDiscovery` now reads local ports from both object and array `Inbound` shapes.
- Added a unit test for the array shape.

Validation after fix:

- `dotnet test .\V2rayN.FlowLens.Tests\V2rayN.FlowLens.Tests.csproj --no-restore` passed: 35 tests.
- `dotnet build .\V2rayN.FlowLens.sln --no-restore` passed: 0 warnings, 0 errors.

### Test Sites

Opened in the browser:

- `https://www.google.com`
- `https://github.com`
- `https://www.baidu.com`

### Observed Logs And Routes

Recent access log examples after opening the test sites:

- `github.githubassets.com:443 [socks >> proxy]`
- `avatars.githubusercontent.com:443 [socks >> proxy]`
- `waa-pa.clients6.google.com:443 [socks -> proxy]`
- `play.google.com:443 [socks -> proxy]`
- `github.com:443 [socks >> proxy]`
- `hectorstatic.baidu.com:443 [socks -> direct]`
- `sp1.baidu.com:443 [socks -> direct]`
- `passport.baidu.com:443 [socks -> direct]`

Result:

- `google.com` / `github.com`: proxy records observed.
- `baidu.com`: direct records observed.

### Observed Applications And Traffic

Windows TCP rows confirmed original applications connecting to `127.0.0.1:10808`, including:

- `msedge.exe` PID `26656`
- `Telegram.exe` PID `24768`
- `Codex.exe` / `codex.exe`

FlowLens Applications tab showed traffic growth after opening the test sites:

- Snapshot around `02:30:55`: top application total `3.46 MB`, all proxy.
- Snapshot around `02:33:59`: top application total `16.18 MB`, all proxy.
- Snapshot around `02:33:59`: another browser-related row total `302.96 KB`, split into `267.85 KB` proxy and `35.11 KB` direct.
- Later snapshots continued showing non-zero totals, for example `20.84 MB` proxy on the top row.

This confirms the ETW byte path is feeding the Applications summary. The application name column is currently too narrow in the screenshot, so some names are truncated; PID/TCP rows were used as supporting evidence.

### Live Connections

Not fully visually confirmed in this run.

Reason:

- Computer Use window capture still failed with `SetIsBorderRequired failed: 不支持此接口 (0x80004002)`.
- Manual coordinate tab switching was unreliable because of desktop scaling and foreground-window behavior.

What was confirmed indirectly:

- ETW session was running.
- Applications tab showed non-zero and increasing traffic values.
- The same `AttributedConnection` model feeds Live Connections `Sent`, `Received`, and `Total`.

Remaining manual check:

- Click the `Live Connections` tab directly in the FlowLens window.
- Confirm rows show non-zero `Sent`, `Received`, and `Total`.

### Current Limits Observed

- Application summary is not long-term cumulative; values can decrease as active/cached connections expire.
- The first column is too narrow, so application names are truncated in the UI.
- Live Connections visual confirmation still needs one direct human UI check.
- TUN remains out of scope and was not tested.

### V1.2 Short Run Verdict

- Pass: Admin/elevated run.
- Pass: ETW session running.
- Pass: access log discovery and parsing.
- Pass: proxy ports include `10808`.
- Pass: source-port route records show proxy and direct.
- Pass: Applications traffic totals increase with browser activity.
- Partial: Live Connections `Sent`/`Received` visual confirmation was not captured.

## Scope

Validate v2rayN FlowLens in normal system proxy mode, without TUN attribution.

## Environment

- v2rayN: `v2rayN - V7.22.6 - X64`
- v2rayN path: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\v2rayN.exe`
- Mode: normal system proxy mode
- TUN: disabled
  - Confirmed from `guiConfigs\guiNConfig.json`: `TunModeItem.EnableTun = false`
- System proxy: enabled
  - Registry `ProxyEnable = 1`
  - Registry `ProxyServer = 127.0.0.1:10808`
- FlowLens process: launched from `V2rayN.FlowLens.App\bin\Debug\net8.0-windows\V2rayN.FlowLens.App.exe`
- FlowLens elevation: not elevated during this run
  - ETW byte accounting is expected to be unavailable or incomplete in this run.

## Local Ports

Observed browser/app traffic used:

- `127.0.0.1:10808`

Other v2rayN-related processes observed:

- `v2rayN.exe`
- `xray.exe`
- `HttpProxy.exe`

## Log Path

Actual v2rayN log directory:

`E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs`

Current core access log:

`E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs\Vaccess_2026-06-13.txt`

Observed access log formats include:

- `from 127.0.0.1:<port> accepted //domain:443 [socks >> proxy]`
- `from 127.0.0.1:<port> accepted //domain:443 [socks >> direct]`

## Test Sites

Opened through the system browser during the test:

- `https://www.google.com`
- `https://github.com`
- `https://www.baidu.com`

## Observed Applications

Windows TCP connection inspection confirmed original applications connecting to the local v2rayN proxy port:

- `chrome.exe` PID `19320` -> `127.0.0.1:10808`
- `msedge.exe` PID `16032` -> `127.0.0.1:10808`
- `msedgewebview2.exe` PID `24480` -> `127.0.0.1:10808`
- `Codex.exe` / `codex.exe` -> `127.0.0.1:10808`

The observed traffic was not collapsed into only `xray.exe`, `sing-box.exe`, or `HttpProxy.exe`.

## Proxy / Direct Attribution

Result: pass at the source-port matching layer.

Confirmed examples:

- `chrome.exe` PID `19320`, source port `6395`
  - Target: `//clients4.google.com:443`
  - Route: `socks -> proxy`
- `msedge.exe` PID `16032`, source port `6502`
  - Target: `//www.google.com:443`
  - Route: `socks -> proxy`
- `msedge.exe` PID `16032`, source port `6509`
  - Target: `//github.githubassets.com:443`
  - Route: `socks -> proxy`
- `msedge.exe` PID `16032`, source port `6515`
  - Target: `//www.baidu.com:443`
  - Route: `socks -> direct`
- `msedge.exe` PID `16032`, source port `6530`
  - Target: `//sp1.baidu.com:443`
  - Route: `socks -> direct`

Latest 500 parsed route records during the run:

- `proxy`: 461
- `direct`: 39

## UI Validation

The WPF app launched successfully.

Computer Use could detect the FlowLens window, but screenshot/text capture failed with:

`SetIsBorderRequired failed: 不支持此接口 (0x80004002)`

Because of that tool-side capture failure, this run validated the same underlying Core data path from the terminal instead of relying on visual UI inspection. Manual UI confirmation still needs one human step:

- Enter `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained` in the log path box.
- Keep proxy ports as `10808,10809`.
- Click `Refresh`.

Expected UI result:

- Applications should include `chrome.exe` and `msedge.exe`.
- Live connections should include matched `proxy` and `direct` rows.
- Status should show active logs including `Vaccess_2026-06-13.txt`.
- ETW status may report unavailable unless FlowLens is run as administrator.

## Current Errors / Limits

- The core attribution chain works for current TCP rows and current access log rows.
- Some short-lived connections can disappear from the current TCP table before the log line is read; V1.1's cache is intended to reduce that.
- This run did not fully verify WPF visual rendering because Computer Use could not capture the FlowLens window.
- This run did not verify ETW byte counts because FlowLens was not elevated.
- TUN attribution was not tested and remains out of scope.

## Validation Commands

- `dotnet build .\V2rayN.FlowLens.sln` passed: 0 warnings, 0 errors.
- `dotnet test .\V2rayN.FlowLens.sln` passed: 23 tests.
- `dotnet run --project .\V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj` launched the WPF app.

## Verdict

Pass for non-TUN source-port attribution.

- Pass: non-TUN system proxy mode is active.
- Pass: original applications such as `chrome.exe` and `msedge.exe` are visible connecting to v2rayN local proxy port `10808`.
- Pass: real access log records can be matched by source port.
- Pass: both `proxy` and `direct` routes were observed.
- Not fully verified: WPF visual inspection and ETW byte accounting.

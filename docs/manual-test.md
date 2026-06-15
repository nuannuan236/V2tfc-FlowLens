# Manual Test - 2026-06-13

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

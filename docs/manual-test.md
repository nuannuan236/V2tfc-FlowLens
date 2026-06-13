# Manual Test - 2026-06-13

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

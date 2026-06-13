# Manual Test - 2026-06-13

## Scope

Validate v2rayN FlowLens MVP in normal system proxy mode, without TUN attribution.

## Environment

- v2rayN: `v2rayN - V7.22.6 - X64`
- v2rayN path: `E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\v2rayN.exe`
- Mode: normal system proxy mode
- TUN: disabled
  - Confirmed from `guiConfigs\guiNConfig.json`: `TunModeItem.EnableTun = False`
  - No active TUN/Wintun adapter was found by adapter name/description search.
- System proxy: enabled
  - Registry `ProxyEnable = 1`
  - Registry `ProxyServer = 127.0.0.1:10808`

## Local Ports

Confirmed listening ports during the test:

- `127.0.0.1:10808` owned by `xray.exe`
- `::1/::`: `10810` owned by `xray.exe`
- `127.0.0.1:10812` owned by `xray.exe`
- `0.0.0.0:10720` owned by `HttpProxy.exe`
- `::`: `10727` owned by `HttpProxy.exe`

FlowLens default ports cover `10808`; the observed browser traffic used `127.0.0.1:10808`.

## Log Path

Actual v2rayN GUI log directory:

`E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs`

Latest file:

`E:\AAA_gong_ju\VPN\v2rayN-windows-64-SelfContained\guiLogs\2026-06-13.txt`

Important finding:

- `CoreBasicItem.LogEnabled = False`
- Current logs contain GUI lifecycle/error lines, but no route lines like:
  - `from 127.0.0.1:xxxxx accepted //domain:443 [socks -> proxy]`
- Search result for accepted route records: `0`

Because no core route log exists, this run cannot validate real `proxy/direct/block` attribution from logs.

## Test Sites

Opened through the system browser during the test:

- `https://www.google.com`
- `https://github.com`
- `https://www.baidu.com`

## Observed Applications

Windows TCP connection inspection confirmed original applications connecting to the local v2rayN proxy port:

- `chrome.exe` PID `19320` -> `127.0.0.1:10808`
- `msedge.exe` PID `11484` and `9804` -> `127.0.0.1:10808`
- `Codex.exe` / `codex.exe` -> `127.0.0.1:10808`
- `svchost.exe` -> `127.0.0.1:10808`

FlowLens UI also showed application summaries for:

- `msedge.exe`
- `chrome.exe`
- `Codex.exe`
- `svchost.exe`

It did not collapse all observed traffic into only `xray.exe`, `sing-box.exe`, or `HttpProxy.exe`.

## Proxy / Direct Attribution

Result: not validated in this real run.

Reason:

- TCP side works: local source ports such as `8205`, `8227`, `9956`, `10649`, `10769`, `10772`, etc. were visible for browser connections to `127.0.0.1:10808`.
- Log side is missing: there are no matching `from 127.0.0.1:<source port> accepted ... [socks -> proxy/direct]` records because core logging is disabled.
- FlowLens therefore displayed these connections as `Unknown`, which is the correct conservative behavior for the current evidence.

## Current Errors / Limits

- This test proves the TCP reader side can identify original browser processes in non-TUN mode.
- This test does not prove real `proxy/direct` routing attribution, because v2rayN core route logging is currently disabled.
- `guiLogs` are not sufficient for source-port attribution when they only contain GUI logs.
- Some short-lived connections may appear as `Idle.exe` / PID `0` after entering `TIME_WAIT`; these should be treated as low-confidence historical rows.
- Reliable byte accounting is still not part of this MVP.
- TUN attribution was not tested and remains out of scope for this version.

## Validation Commands

- `dotnet test .\V2rayN.FlowLens.sln` passed: 4 tests.
- `dotnet build .\V2rayN.FlowLens.sln` passed: 0 warnings, 0 errors.
- `dotnet run --project .\V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj` launched the WPF app successfully.

## Verdict

Partial pass.

- Pass: non-TUN system proxy mode is active.
- Pass: original applications such as `chrome.exe` and `msedge.exe` are visible connecting to v2rayN local proxy port `10808`.
- Pass: FlowLens UI shows those original applications instead of only core processes.
- Blocked: real `proxy/direct` attribution cannot be verified until v2rayN/Xray route logs with `accepted ... [inbound -> outbound]` records are enabled or otherwise available.

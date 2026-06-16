# Current Project State

## v2rayN FlowLens V1.3.1

Status: active
Updated: 2026-06-16
Confidence: high
Scope: project

Canonical memory entrypoint is `memory-bank/current.md`. Older planning notes, if added later, should point here rather than becoming competing active context.

The project is a Windows desktop tool for v2rayN proxy traffic attribution. The current scope remains non-TUN system proxy mode only: read v2rayN/Xray access logs, read Windows TCP/ETW events, and attribute local proxy-port connections back to the original application by matching the application's ephemeral source port with access log lines such as `from 127.0.0.1:<port>`.

Current implementation status: V1.3 exists with V1.2 diagnostics/settings plus V1.2.1 receive attribution cleanup and V1.3 tray residency/refresh controls. Latest committed code includes tray Show/Refresh/Pause/Exit behavior, pause/resume refresh, settings compatibility, and ETW receive fallback.

Current traffic scope: ETW counts bytes for the application-to-local-v2rayN-proxy leg only, keyed by PID/local port/remote port. It does not count the core outbound leg to the proxy node and does not claim parity with Windows Data Usage or ISP billing.

Current repository state: latest feature commit before V1.3.1 documentation is `3ce1797` (`Add tray residency and refresh controls`). Working tree was clean before the V1.3.1 reference-analysis documentation work.

Next action: use V1.3.1 reference analysis to choose V1.4. Default recommendation is OpenNetMeter-style "this run" totals first, with WhoYouCalling-style DNS ETW enrichment kept as a separate later investigation.

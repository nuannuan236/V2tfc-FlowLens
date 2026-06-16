# Current Project State

## v2rayN FlowLens Current Roadmap Position

Status: active
Updated: 2026-06-16
Confidence: high
Scope: project

Canonical memory entrypoint is `memory-bank/current.md`. Older planning notes, if added later, should point here rather than becoming competing active context.

The project is a Windows desktop tool for v2rayN proxy traffic attribution. It is now beyond the initial MVP and is in the middle-to-late stage of a usable non-TUN normal proxy mode version. The current scope remains non-TUN system proxy mode only: read v2rayN/Xray access logs, read Windows TCP/ETW events, and attribute local proxy-port connections back to the original application by matching the application's ephemeral source port with access log lines such as `from 127.0.0.1:<port>`.

Completed roadmap position:

- V0 log panel: complete
- V1 normal proxy mode attribution: complete
- V1.1 stability + ETW traffic: complete
- V1.2 diagnostics + settings persistence: complete
- V1.2.1 closeout fixes: complete
- V1.3 tray residency + refresh controls: complete
- V1.3.1 reference project audit: complete
- V1.4 this-run session statistics: complete
- V1.4.1 session CSV export: complete
- V1.5 today statistics / local aggregate history: complete
- V2 TUN attribution: not started
- V3 long-term statistics / anomaly alerts: not started

Current capabilities include original application recognition (`chrome.exe`, `msedge.exe`, etc.), source-port matching to v2rayN access logs, `proxy`/`direct`/`unknown` display, ETW traffic for the app-to-local-proxy leg, Diagnostics self-checks, persisted settings, tray residency, pause/manual refresh, this-run Session statistics, manual Session CSV export, Today aggregate statistics, and reference-project/license boundary notes.

Current traffic scope: ETW counts bytes for the application-to-local-v2rayN-proxy leg only, keyed by PID/local port/remote port. It does not count the core outbound leg to the proxy node and does not claim parity with Windows Data Usage or ISP billing.

Current repository state: V1.5 is treated as completed in project planning. Check `git status` before committing or starting the next feature because local implementation state may be ahead of the last recorded memory entry.

Next action: do not jump to TUN yet. Prefer V1.5.x validation/cleanup or small usability work after a real administrator manual validation pass. TUN attribution remains a later V2 because it is a difficulty jump and should wait until the non-TUN experience is stable.

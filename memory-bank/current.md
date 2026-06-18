# Current Project State

## v2rayN FlowLens Current Roadmap Position

Status: active
Updated: 2026-06-16
Confidence: high
Scope: project

Canonical memory entrypoint is `memory-bank/current.md`. Older planning notes, if added later, should point here rather than becoming competing active context.

The project is a Windows desktop tool for v2rayN proxy traffic attribution. It now supports the mature Normal Proxy path plus an opt-in first TUN attribution mode. Normal Proxy remains the default and uses precise source-port matching. TUN mode is conservative and evidence-based, using Windows TCP/ETW data, route logs, target IP/port, and a short time window to produce `Matched`, `Probable`, `Ambiguous`, or `Unknown` results.

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
- V1.6 pre-TUN closeout usability: complete
- V2 TUN attribution: first implementation complete, real desktop TUN validation pending
- V3 long-term statistics / anomaly alerts: not started

Current capabilities include original application recognition (`chrome.exe`, `msedge.exe`, etc.), Normal Proxy source-port matching to v2rayN access logs, opt-in conservative TUN attribution, `proxy`/`direct`/`unknown` display, ETW traffic, Diagnostics self-checks, persisted settings, tray residency, pause/manual refresh, this-run Session statistics, manual Session CSV export, Today aggregate statistics, History viewing/export for local daily aggregate files, lightweight UI filtering, copyable diagnostics, and reference-project/license boundary notes.

Current traffic scope: Normal Proxy ETW counts bytes for the application-to-local-v2rayN-proxy leg only. TUN mode asks ETW to observe broader TCP flows for candidate matching. Neither mode captures packets, handles UDP, or claims parity with Windows Data Usage or ISP billing.

Current repository state: V2 first TUN implementation is treated as completed in project planning. Check `git status` before committing or starting the next feature because local implementation state may be ahead of the last recorded memory entry.

Next action: run real administrator validation for both Normal Proxy regression and TUN mode. Expect TUN to produce conservative `Probable`, `Ambiguous`, and `Unknown` results; do not treat these as bugs unless the evidence chain is clearly wrong.

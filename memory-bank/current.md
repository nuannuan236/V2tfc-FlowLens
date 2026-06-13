# Current Project State

## v2rayN FlowLens V1.2

Status: active
Updated: 2026-06-14
Confidence: high
Scope: project

Canonical memory entrypoint is `memory-bank/current.md`. Older planning notes, if added later, should point here rather than becoming competing active context.

The project is a Windows desktop tool for v2rayN proxy traffic attribution. The current scope remains non-TUN system proxy mode only: read v2rayN/Xray access logs, read Windows TCP connections, and attribute local proxy-port connections back to the original application by matching the application's ephemeral source port with access log lines such as `from 127.0.0.1:<port>`.

Current implementation status: V1.2 prototype exists with V1.1 attribution stability, real `Vaccess_*.txt` compatibility, Windows TCP connection reader, 120-second connection snapshot cache, ETW traffic monitor, source-port attribution, settings persistence, read-only v2rayN config discovery, and a WPF Diagnostics tab.

Current traffic scope: ETW counts bytes for the application-to-local-v2rayN-proxy leg only, keyed by PID/local port/remote port. It does not count the core outbound leg to the proxy node and does not claim parity with Windows Data Usage or ISP billing.

Current repository state: Git was initialized at `2f6542f` (`Initial FlowLens MVP`). V1.1 stability/log discovery was committed as `846a371` (`Add V1.1 attribution stability and log discovery fixes`). The latest V1.2 work uses commit message `Add V1.2 diagnostics and settings persistence`.

Next action: manually validate V1.2 with v2rayN in non-TUN mode: first non-admin for diagnostics/degraded ETW messaging, then elevated for ETW byte growth while browsing.

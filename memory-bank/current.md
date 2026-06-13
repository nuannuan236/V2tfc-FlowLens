# Current Project State

## v2rayN FlowLens V1.1

Status: active
Updated: 2026-06-13
Confidence: high
Scope: project

Canonical memory entrypoint is `memory-bank/current.md`. Older planning notes, if added later, should point here rather than becoming competing active context.

The project is a Windows desktop tool for v2rayN proxy traffic attribution. The current scope remains non-TUN system proxy mode only: read v2rayN/Xray access logs, read Windows TCP connections, and attribute local proxy-port connections back to the original application by matching the application's ephemeral source port with access log lines such as `from 127.0.0.1:<port>`.

Current implementation status: V1.1 prototype exists with a log parser compatible with real `Vaccess_*.txt` variants, Windows TCP connection reader, 120-second connection snapshot cache, ETW traffic monitor, source-port attribution engine, minimal WPF dashboard, tests, README, and a Git repository.

Current traffic scope: ETW counts bytes for the application-to-local-v2rayN-proxy leg only, keyed by PID/local port/remote port. It does not count the core outbound leg to the proxy node and does not claim parity with Windows Data Usage or ISP billing.

Current repository state: Git was initialized and committed as `2f6542f` with message `Initial FlowLens MVP`; working tree was clean after the commit.

Next action: manually validate V1.1 with v2rayN in non-TUN mode, Core access log enabled, and FlowLens started as administrator so ETW traffic statistics are available.

# Current Project State

## v2rayN FlowLens V1.4

Status: active
Updated: 2026-06-16
Confidence: high
Scope: project

Canonical memory entrypoint is `memory-bank/current.md`. Older planning notes, if added later, should point here rather than becoming competing active context.

The project is a Windows desktop tool for v2rayN proxy traffic attribution. The current scope remains non-TUN system proxy mode only: read v2rayN/Xray access logs, read Windows TCP/ETW events, and attribute local proxy-port connections back to the original application by matching the application's ephemeral source port with access log lines such as `from 127.0.0.1:<port>`.

Current implementation status: V1.4 adds in-memory "this run" session statistics on top of V1.3 tray residency/refresh controls. Latest code includes session application/domain summaries, Reset Session, session started display, and delta-based accumulation to avoid recounting the same live connection every refresh.

Current traffic scope: ETW counts bytes for the application-to-local-v2rayN-proxy leg only, keyed by PID/local port/remote port. It does not count the core outbound leg to the proxy node and does not claim parity with Windows Data Usage or ISP billing.

Current repository state: V1.3.1 reference analysis was committed as `66511e7` (`Add reference analysis for network monitor projects`). V1.4 session statistics are currently implemented in the working tree pending final validation and commit.

Next action: validate V1.4 with build/test and a short administrator manual run: browse through v2rayN, confirm live rows can disappear while Session totals remain, then confirm Reset Session clears only the in-memory session totals.

# Current Project State

## v2rayN FlowLens MVP

Status: active
Updated: 2026-06-13
Confidence: high
Scope: project

Canonical memory entrypoint is `memory-bank/current.md`. Older planning notes, if added later, should point here rather than becoming competing active context.

The project is a Windows desktop tool for v2rayN proxy traffic attribution. The MVP targets non-TUN system proxy mode only: read v2rayN/Xray/sing-box logs, read current Windows TCP connections, and attribute local proxy-port connections back to the original application by matching the application's ephemeral source port with log lines such as `from 127.0.0.1:<port>`.

Current implementation status: initial .NET 8 WPF prototype exists with a log parser, Windows TCP connection reader, attribution engine, minimal dashboard, tests, and README.

Current constraint: reliable per-connection byte accounting is not implemented. The first version shows attribution and connection counts, with `N/A` for estimated traffic.

Next action: run the WPF app against a real v2rayN non-TUN session and sample logs, then adjust parser coverage for any real log variants.

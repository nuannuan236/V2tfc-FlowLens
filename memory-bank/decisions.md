# Decisions

## Use C# + .NET 8 + WPF

Status: active
Updated: 2026-06-13
Confidence: high
Scope: technology

Decision: build the first prototype with C#, .NET 8, and WPF.

Rationale: the app is Windows-only, needs process/TCP access, and benefits from native desktop integration.

## Install SDK Before Scaffolding

Status: active
Updated: 2026-06-13
Confidence: high
Scope: setup

Decision: install .NET 8 SDK before project creation, then use official templates instead of hand-writing the initial WPF project files.

Rationale: templates reduce framework and XAML boilerplate risk.

## Add ETW Traffic Accounting

Status: active
Updated: 2026-06-13
Confidence: high
Scope: attribution

Decision: V1.1 adds ETW-based byte accounting for application connections to local v2rayN proxy ports.

Rationale: `GetExtendedTcpTable` provides current TCP rows but not reliable byte counters. ETW gives a practical user-mode way to count send/receive bytes for the local proxy-entry leg while preserving the project boundary of no driver, no packet capture, and no v2rayN config modification.

## Keep TUN Out Of V1.1

Status: active
Updated: 2026-06-13
Confidence: high
Scope: roadmap

Decision: V1.1 remains limited to non-TUN system proxy mode.

Rationale: TUN attribution needs approximate correlation across system-level capture, routing logs, IP/domain/time windows, and shared processes. It should not be mixed into the first traffic-statistics version.

## Persist Only Non-Sensitive Settings

Status: active
Updated: 2026-06-14
Confidence: high
Scope: privacy

Decision: V1.2 persists UI settings as JSON under `%LocalAppData%\V2rayN.FlowLens\settings.json`, but does not store log contents, domain history, subscriptions, nodes, accounts, or credentials.

Rationale: remembered local paths, ports, refresh interval, and filters improve usability without expanding the privacy surface.

## Treat v2rayN Config Discovery As Read-Only Assistance

Status: active
Updated: 2026-06-14
Confidence: high
Scope: configuration

Decision: V1.2 reads `guiConfigs\guiNConfig.json` to discover `Inbound.LocalPort`, appending discovered ports to user-entered ports instead of replacing them.

Rationale: automatic discovery reduces setup mistakes, but user-entered ports must remain authoritative enough to cover mixed/http/socks variants not represented by the current config shape.

## Use Reference Projects Without Forking

Status: active
Updated: 2026-06-16
Confidence: high
Scope: roadmap

Decision: V1.3.1 uses OpenNetMeter, WhoYouCalling, Sniffnet, Portmaster, and simplewall as references, but keeps FlowLens' existing architecture and does not vendor third-party source.

Rationale: FlowLens' distinctive value is v2rayN route attribution. Existing network monitors help with traffic history, ETW/DNS ideas, and UX, but direct adoption would either miss v2rayN semantics or introduce license/scope risk.

## Start History With In-Memory Session Totals

Status: active
Updated: 2026-06-16
Confidence: high
Scope: roadmap

Decision: V1.4 implements "this run" traffic totals in memory before adding today/week/month persistence or CSV export.

Rationale: session totals deliver immediate value and validate the accumulation model without introducing database, retention, export, or privacy decisions too early.

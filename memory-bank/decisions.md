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

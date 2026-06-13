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

## Delay Reliable Byte Accounting

Status: active
Updated: 2026-06-13
Confidence: high
Scope: attribution

Decision: the first implementation may show connection counts and a placeholder/estimated traffic field, but reliable per-connection byte accounting is not required.

Rationale: `GetExtendedTcpTable` provides current TCP rows but not reliable per-connection byte counters. ETW should be considered after source-port attribution works.

# Stable Project Knowledge

## Product Boundary

Status: active
Updated: 2026-06-13
Confidence: high
Scope: product

FlowLens is a read-only attribution panel for v2rayN proxy traffic. It must not replace v2rayN, edit v2rayN settings, manage subscriptions or nodes, upload logs/domains, or behave as a full packet sniffer.

## MVP Mode

Status: active
Updated: 2026-06-13
Confidence: high
Scope: product

The first useful version supports normal non-TUN system proxy mode. TUN attribution is explicitly V2 work because it needs approximate correlation and has higher error risk.

## Attribution Rule

Status: active
Updated: 2026-06-13
Confidence: high
Scope: data-flow

In normal proxy mode, an application connecting to a configured local v2rayN proxy port, such as `127.0.0.1:10808` or `127.0.0.1:10809`, is considered to have entered the proxy. The application's local ephemeral port is matched to log records containing `from 127.0.0.1:<ephemeral-port>` to recover target and outbound result.

## Real Access Log Variants

Status: active
Updated: 2026-06-13
Confidence: high
Scope: log-parser

Real v2rayN/Xray access logs may be named `Vaccess_YYYY-MM-DD.txt` under `guiLogs`. Useful routing lines can include millisecond timestamps, `from tcp:127.0.0.1:<port>`, targets like `//domain:443` or `tcp:domain:443`, and route syntax using either `[socks -> proxy]` or `[socks >> proxy]`.

## Traffic Measurement Scope

Status: active
Updated: 2026-06-13
Confidence: high
Scope: traffic

V1.1 traffic statistics use ETW TCP/IP events for the application-to-local-proxy connection leg. ETW needs administrator privileges. If ETW is unavailable, attribution and proxy/direct parsing should still work, but byte counters remain unavailable or zero.

## v2rayN Config Discovery

Status: active
Updated: 2026-06-14
Confidence: high
Scope: configuration

V1.2 discovers local proxy ports from `guiConfigs\guiNConfig.json`, currently using `Inbound.LocalPort`. Discovery is read-only and should append to user-entered ports rather than overwrite them.

## Unknown Policy

Status: active
Updated: 2026-06-13
Confidence: high
Scope: attribution

Do not force attribution when evidence is insufficient. Use `unknown` instead of guessing.

## Reference Project Boundary

Status: active
Updated: 2026-06-16
Confidence: high
Scope: research

V1.3.1 reviewed OpenNetMeter, WhoYouCalling, Sniffnet, Portmaster, and simplewall as references. FlowLens should not fork or vendor these projects. Apache-2.0/MIT projects may inform implementation patterns if source is credited and license compatibility is checked; GPL-3.0 projects are product/architecture references only and source code must not be copied into FlowLens.

## Session Statistics Scope

Status: active
Updated: 2026-06-16
Confidence: high
Scope: traffic

V1.4 session statistics are in-memory "this run" totals only. They accumulate positive byte deltas from attributed rows with process context, exclude `LogOnly`, and reset on app exit or `Reset Session`. They do not persist history or change the ETW/local-proxy-entry traffic scope.

## Session CSV Export Scope

Status: active
Updated: 2026-06-16
Confidence: high
Scope: export

V1.4.1 Session CSV export is manual and one-shot. It writes Applications or Domains CSV only to a user-selected path, uses UTF-8 with BOM, exports raw integer byte fields, and does not create a long-term history database or automatic background log.

## Today Aggregate History Scope

Status: active
Updated: 2026-06-16
Confidence: high
Scope: persistence

V1.5 Today statistics persist only per-day aggregate Applications and Domains summaries under `%LocalAppData%\V2rayN.FlowLens\history\yyyy-MM-dd.json`. They do not persist raw connections, full logs, subscriptions, nodes, accounts, or credentials. `Reset Session` must not clear Today history.

## V1.6 History And Filtering Scope

Status: active
Updated: 2026-06-16
Confidence: high
Scope: usability

V1.6 adds a History tab for local daily aggregate JSON files, Today/History CSV export, display-only keyword/outbound filtering, default administrator startup through an app manifest, and copyable diagnostics. Filters must not mutate Live attribution, Session accumulation, Today persistence, or History files.

## Near-Term Roadmap Boundary

Status: active
Updated: 2026-06-16
Confidence: high
Scope: planning

After V1.6, non-TUN normal proxy mode has enough daily-use functionality. V2 TUN attribution is now implemented as an opt-in conservative mode and must be validated as approximate correlation, not exact attribution.

## TUN Attribution Policy

Status: active
Updated: 2026-06-18
Confidence: high
Scope: attribution

TUN mode uses a +/-5 second matching window over Windows TCP candidates and route log evidence. Exact target IP/port matches can be `Matched`; domain-only evidence can only be `Probable` when a single candidate matches by time and port; multiple candidates must be `Ambiguous`; missing evidence must be `Unknown`. `Ambiguous` and `Unknown` must not be counted as confirmed application traffic.

## TUN Duplicate Evidence Policy

Status: active
Updated: 2026-06-18
Confidence: high
Scope: attribution

Within one refresh, a TUN TCP candidate may be consumed by at most one route-evidence row. Later route evidence that only matches already-consumed candidates is treated as duplicate evidence and skipped, not emitted as another attributed row or zero-byte `Unknown`. TUN mode also honors `OnlyShowProxy` before returning visible rows.

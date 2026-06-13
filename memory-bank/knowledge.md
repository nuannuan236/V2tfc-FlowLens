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

# Pitfalls

## Core Access Log Required

Status: active
Updated: 2026-06-13
Confidence: high
Scope: validation

If v2rayN Core access logging is not enabled, FlowLens can still identify applications connected to local proxy ports, but cannot determine `proxy`/`direct` routing. Seeing only GUI logs is not enough; useful lines look like `from ... accepted ... [inbound -> outbound]` or `from ... accepted ... [inbound >> outbound]`.

## Short-Lived TCP Rows

Status: active
Updated: 2026-06-13
Confidence: high
Scope: attribution

Browser proxy connections may disappear from `GetExtendedTcpTable` before the corresponding access log is read. V1.1 keeps a short connection snapshot cache to reduce this race, but stale cache matches should still be treated as recent attribution evidence rather than perfect historical truth.

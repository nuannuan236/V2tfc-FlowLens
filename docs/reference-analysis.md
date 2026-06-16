# Reference Analysis - V1.3.1

Updated: 2026-06-16

This note records comparable network-monitoring projects and what FlowLens should or should not absorb from them. The goal is to reduce wheel-building without changing FlowLens into a general firewall, packet sniffer, or v2rayN replacement.

FlowLens keeps its current differentiator:

```text
v2rayN/Xray access log + Windows TCP/ETW + source-port attribution = app -> proxy/direct
```

## Comparison

| Project | Repository | License | Position | Useful for FlowLens | Do not absorb |
| --- | --- | --- | --- | --- | --- |
| OpenNetMeter | https://github.com/Ashfaaq18/OpenNetMeter | Apache-2.0 | Windows data-usage monitor with process/session/history views. | Session/today totals, historical retention shape, administrator messaging, install/startup experience. | Do not replace FlowLens attribution; it is not v2rayN route-aware. |
| WhoYouCalling | https://github.com/H4NM/WhoYouCalling | MIT | Windows process network activity tool using ETW TCP/IP and DNS ideas, with optional capture-oriented features. | ETW event handling, process-to-domain enrichment, DNS correlation feasibility, privilege limitations. | Do not import pcap/Npcap capture flow into the MVP line. |
| Sniffnet | https://github.com/GyulyVGC/sniffnet | MIT OR Apache-2.0 | Cross-platform network monitor with mature UX for filtering, notifications, and overview screens. | Product UX patterns: filters, notifications, readable summaries, status presentation. | Do not port Rust implementation or move FlowLens toward generic packet capture. |
| Portmaster | https://github.com/safing/portmaster | GPL-3.0 | Full application firewall and privacy network monitor. | Product explanation, permission-state wording, per-app network mental model. | Do not copy source or implementation because GPL-3.0 would change project obligations. |
| simplewall | https://github.com/henrypp/simplewall | GPL-3.0 | Lightweight Windows Filtering Platform firewall. | WFP concepts and firewall UX references. | Do not copy source; WFP enforcement is outside current read-only scope. |

## Absorbable Ideas

- From OpenNetMeter: build V1.4 around "this run" and "today" traffic totals before long-term history. Keep storage small and explain that bytes are FlowLens' local-proxy-entry scope, not billing-grade totals.
- From WhoYouCalling: evaluate ETW DNS events as a future enrichment path when v2rayN logs are missing target names or when source-port matching has timing gaps.
- From Sniffnet: improve overview pages and filters only after the attribution model remains stable. Prefer simple filters over a new dashboard redesign.
- From Portmaster and simplewall: borrow wording and diagnostic concepts for privilege, firewall-like responsibility, and user trust, but not code.

## Non-Goals

- Do not fork any reference project.
- Do not vendor third-party source into this repository for V1.3.1.
- Do not add databases, DNS ETW, WFP, Npcap, WinDivert, packet capture, or TUN attribution in this research version.
- Do not copy GPL-3.0 implementation code. GPL projects are product and architecture references only.

## V1.4 Recommendation

The next feature version should use OpenNetMeter as the main product reference and stay inside the existing FlowLens data model:

1. Add "this run" totals for applications, domains, and proxy/direct/unknown.
2. Add "today" totals only after a small local persistence design is agreed.
3. Add CSV export after the totals are stable.
4. Keep DNS ETW as an investigation branch, not the default V1.4 path.

WhoYouCalling should be revisited separately for DNS enrichment. That work should remain optional and should not introduce packet capture.

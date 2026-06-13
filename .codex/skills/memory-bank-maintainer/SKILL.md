---
name: memory-bank-maintainer
description: Maintain structured project memory and memory-bank files. Use when the user asks to update memory, organize or split a memory bank, archive historical notes, mark stale knowledge, improve context recovery, finish work by deciding what should be remembered, summarize project progress into durable memory, or convert append-only notes into layered current facts, stable knowledge, and history.
---

# Memory Bank Maintainer

Maintain project memory so future agents read less, read the right files, and do not treat stale notes as current truth.

## Common Actions

Use these action shapes when the user asks for a concrete memory-bank operation:

| Action | Use when | Expected behavior |
| --- | --- | --- |
| Status | User asks what memory exists or whether the memory bank is healthy | List likely entrypoints, current structure, stale-risk areas, and the next useful maintenance step. |
| Init | User asks to create or upgrade a memory bank | Propose or create the smallest useful structure, then classify existing memory into current facts, stable knowledge, and history. |
| Update | User asks to update memory after work, sync, or a decision | Replace current facts, add durable knowledge only when it passes write admission, and archive old state. |
| Archive | User asks to clean up, compress, or move old memory | Move dated or no-longer-current content to history and mark stale items instead of leaving them in active context. |
| Finish | User finished a task and wants memory captured | Decide whether anything durable changed, then update current state, history, pitfalls, decisions, or knowledge only when needed. |

Do not treat these as commands that must be present in the user request. They are intent categories for natural-language requests.

## Workflow

1. Locate the memory entrypoint before editing:
   - Prefer `memory-bank/current.md`, then `memory/current.md`, then `memory.md`.
   - Also inspect `AGENTS.md` or project instructions when they mention memory rules.
2. Classify the requested update:
   - Current state: active project phase, latest status, immediate constraints, next action.
   - Stable knowledge: durable rules, verified commands, architecture notes, workflow preferences.
   - History: dated progress logs, old milestones, completed sync records, prior experiments.
   - Pitfall: repeated mistake, known failure mode, misleading command, recovery note.
   - Decision: important choice, reason, tradeoff, and current status.
3. Read only the files needed for that class of update.
4. Prefer replacing current facts over appending. Move old facts to history or mark them stale.
5. Preserve user-authored unrelated content. If existing content is ambiguous, ask before rewriting it.
6. End with a concise report of files changed, facts replaced, items archived, and stale notes marked.

## Finish Pass

Use a finish pass at the end of a task, sync, fix, release, or investigation. The goal is not to write a summary of everything. The goal is to decide what future work actually needs.

Ask these questions:

- Current state: did the active project phase, latest status, constraint, or next action change?
- Stable knowledge: was a durable fact or verified workflow learned?
- Pitfall: was there a repeated mistake, misleading assumption, or recovery step worth preserving?
- Decision: was an important choice made with a reason that future agents should know?
- History: is there a completed milestone or old state that should move out of active context?
- No-op: is this a one-off detail that should not be written?

Do not create a `spec.md` layer or turn memory into mandatory coding rules. Keep memory descriptive, reviewable, and easy to revise.

## Read Decision Table

Use the smallest sufficient read set.

| User intent | Read first | Read only if needed |
| --- | --- | --- |
| Current progress, resume context, "what is the state?" | `current.md` or `memory.md` | `history/YYYY-MM.md` |
| Update project memory after work | current entrypoint | relevant stable files, current month history |
| Organize or split a memory bank | existing memory file | `AGENTS.md`, nearby history files |
| Tooling, commands, setup, deployment | current entrypoint | `commands.md`, `pitfalls.md` |
| API, data shape, contracts | current entrypoint | `contracts.md`, `decisions.md` |
| Error, regression, repeated mistake | current entrypoint | `pitfalls.md`, relevant history |
| Why a choice was made | current entrypoint | `decisions.md`, relevant history |
| Retrospective or audit | current entrypoint | history files in the requested date range |
| Finish work or wrap up a task | current entrypoint | `knowledge.md`, `pitfalls.md`, `decisions.md`, current month history |

Do not read the full memory bank by default. History is an archive, not the working context.

## Write Rules

Use the minimum structure that fits the project:

- Small project: `current.md`, `knowledge.md`, `history/YYYY-MM.md`.
- Growing project: add `pitfalls.md`, `decisions.md`, `commands.md`, or `contracts.md` only when those topics are recurring.
- Do not over-split a memory bank just because templates exist.

Write destinations:

- `current.md`: short active truth, current constraints, latest status, next action. Keep it brief.
- `knowledge.md`: stable rules, verified preferences, durable project facts.
- `history/YYYY-MM.md`: dated progress logs and old states that should not load by default.
- `pitfalls.md`: reusable lessons from repeated failures or misleading assumptions.
- `decisions.md`: decisions with rationale, status, and date.
- `commands.md`: verified commands, expected working directory, and caveats.
- `contracts.md`: APIs, schemas, data contracts, integration assumptions.

## Entry Format

Prefer compact entries with explicit freshness:

```markdown
## Title

Status: active | uncertain | deprecated | historical
Updated: YYYY-MM-DD
Confidence: high | medium | low
Scope: project/module/workflow

Fact or rule in one short paragraph.
```

Use `active` only for current truth. Use `uncertain` for unverified claims. Use `deprecated` for old guidance that may explain history but should not be followed. Use `historical` for archive-only context.

## Write Admission

Write memory only when it is likely to help future work.

Allowed:

- User-confirmed long-term preference.
- Verified command, setup step, or recovery procedure.
- Current project state needed for context recovery.
- Repeated pitfall or recurring mistake.
- Durable decision, tradeoff, or rule.
- Historical milestone or completed sync record.

Avoid:

- One-off chat details.
- Raw temporary logs.
- Unverified AI guesses presented as fact.
- Old plans without status.
- Content already represented clearly elsewhere.
- Large copied documents when a short summary and source path are enough.

## Safety Rules

Never store raw secrets:

- `.env` contents
- API keys
- tokens
- cookies
- SSH keys
- private relay keys
- account credentials

It is acceptable to store environment variable names, non-secret examples, configuration purpose, error types, and decision rationale.

## Templates

For copy-ready structures, read `references/templates.md` only when creating or reorganizing memory-bank files.

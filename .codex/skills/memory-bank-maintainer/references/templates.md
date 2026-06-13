# Memory Bank Templates

Use these templates only when creating or reorganizing memory files. Keep small projects small.

## Minimal Structure

```text
memory-bank/
  current.md
  knowledge.md
  history/
    YYYY-MM.md
```

## Expanded Structure

Add these files only when the content is recurring enough to justify the split:

```text
memory-bank/
  current.md
  knowledge.md
  commands.md
  contracts.md
  decisions.md
  pitfalls.md
  history/
    YYYY-MM.md
```

## current.md

```markdown
# Current Context

Updated: YYYY-MM-DD
Status: active

## Current State

- Project phase:
- Latest completed work:
- Current constraints:
- Next useful action:

## Read Next

- For commands:
- For decisions:
- For pitfalls:
- For history:
```

## knowledge.md

```markdown
# Stable Knowledge

## Topic

Status: active
Updated: YYYY-MM-DD
Confidence: high | medium | low
Scope:

Durable rule or fact. Keep this short and remove stale versions instead of appending duplicates.
```

## history/YYYY-MM.md

```markdown
# History - YYYY-MM

## YYYY-MM-DD

- What changed:
- Why it mattered:
- Moved from current:
- Follow-up:
```

## pitfalls.md

```markdown
# Pitfalls

## Short Pitfall Title

Status: active
Updated: YYYY-MM-DD
Confidence: high | medium | low
Scope:

Symptom:

Cause:

Correct handling:
```

## decisions.md

```markdown
# Decisions

## Decision Title

Status: active | deprecated | historical
Date: YYYY-MM-DD
Scope:

Decision:

Reason:

Tradeoff:
```

## commands.md

````markdown
# Commands

## Command Purpose

Status: active
Updated: YYYY-MM-DD
Working directory:

```shell
command here
```

Notes:
````

## contracts.md

```markdown
# Contracts

## Contract Name

Status: active
Updated: YYYY-MM-DD
Scope:

Producer:

Consumer:

Shape or invariant:

Compatibility notes:
```

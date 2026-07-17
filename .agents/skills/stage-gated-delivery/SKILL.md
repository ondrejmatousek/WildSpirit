---
name: stage-gated-delivery
description: Use when implementing a project plan divided into numbered stages or phases. Execute only the explicitly approved stage, update project documentation, verify it, summarize changes, and stop before the next stage.
---

# Stage-Gated Delivery

## Scope

- Read `IMPLEMENTATION_PLAN.md`, `ARCHITECTURE.md`, `DATA_MODEL.md`, `DECISIONS.md`, and `AGENTS.md`.
- Identify the currently approved stage.
- Do not implement later-stage features merely because they are adjacent.
- Small prerequisite fixes are allowed only when necessary; report them.
- Do not rewrite already working architecture without a clear reason.

## Workflow

1. Inspect the current repository and git status.
2. Verify the previous stage still builds and starts.
3. Write a brief internal implementation checklist for the approved stage.
4. Implement in coherent increments.
5. Add migrations and tests as needed.
6. Update documentation and decisions.
7. Run restore, build, tests, migrations, and runtime verification.
8. Review the diff for accidental unrelated changes.
9. Summarize completion and known limitations.
10. Stop.

## Completion report

Include:

- implemented scope,
- important files,
- database changes,
- dependencies added,
- decisions made,
- tests added,
- verification commands and outcomes,
- known limitations,
- suggested next stage.

Do not begin the suggested stage until the user explicitly approves it.

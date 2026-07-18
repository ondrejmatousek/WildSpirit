---
name: stage-gated-delivery
description: Use only when implementing an approved numbered stage from IMPLEMENTATION_PLAN.md. Stay within the approved scope, verify the work appropriately, summarize the completed stage, and stop before the next stage.
---

# Stage-Gated Delivery

## Rules

- Read only the documents relevant to the current stage.
- Implement only the approved stage.
- Do not implement future-stage features.
- Keep changes focused and avoid unrelated refactoring.

## During implementation

- Work in small coherent commits.
- Add migrations only if the data model changes.
- Add or update tests only for changed behavior.
- Update documentation only if architecture, data model or public behavior changed.

## Verification

Choose verification proportional to the work.

Small stage:
- Build affected projects.
- Run relevant tests.

Large stage:
- Build solution.
- Run relevant tests.
- Verify migrations when schema changed.
- Start the application only when behavior requires runtime verification.

## Completion

Report:

- implemented scope
- important files
- database changes (if any)
- tests added (if any)
- known limitations
- suggested next stage

Stop after the approved stage.
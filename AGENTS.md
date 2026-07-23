# DesignStore

## Project

Production ASP.NET Core MVC ecommerce application.

## Rules

- Respect ARCHITECTURE.md and DATA_MODEL.md when relevant.
- Keep controllers thin.
- Keep business logic outside Web and Razor.
- Use async APIs and CancellationToken where applicable.
- Use decimal for money.
- Prevent overselling.
- Preserve historical snapshots.
- Never expose internal information.

## Scope

- Modify only what the task requires.
- Avoid unrelated refactoring.
- Do not expand the requested scope.
- Do not continue to another stage automatically.
- Do not perform additional investigations unless required to complete the task.
- Reuse existing architecture and services.

## Verification

Use the smallest verification that provides reasonable confidence.

Small change:
- Build the affected project only.

Functional change:
- Build the affected project.
- Run relevant targeted tests.

Schema, security, or stage completion:
- Build the solution.
- Run relevant tests.
- Verify migrations when applicable.

Do not perform end-to-end or runtime verification unless explicitly requested or necessary to complete the task.

## Skills

Use only the skills relevant to the current task.
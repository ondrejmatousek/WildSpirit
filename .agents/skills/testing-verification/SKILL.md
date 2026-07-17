---
name: testing-verification
description: Use after implementing or modifying application behavior, configuration, dependencies, database schema, routing, Razor views, or infrastructure. Requires targeted tests, full build, and real application startup verification before declaring work complete.
---

# Testing and Verification

## Test strategy

- Add tests for new business rules and regressions.
- Prefer unit tests for pure rules and integration tests for EF Core, authorization, routing, and transactions.
- Test negative and boundary cases.
- Keep tests deterministic and independent.
- Do not use mocks where a small real component or test database gives more confidence.
- Name tests by behavior and condition.

## Required verification sequence

1. Read repository instructions for canonical commands.
2. Restore dependencies when needed.
3. Build the affected project or whole solution.
4. Run relevant tests.
5. Run the full test suite when practical.
6. Apply migrations to a disposable/development database when schema changed.
7. Start the real web application using documented development configuration.
8. Observe startup logs.
9. Probe a health endpoint or representative local URL.
10. Stop only the process started for verification.

A process existing is not proof that startup succeeded.

## Reporting

State exactly:

- commands run,
- build result,
- test count and result,
- runtime route or health check verified,
- warnings not fixed and why,
- checks not run and why.

Never claim completion when build, tests, migration, or startup failed. Fix failures within scope before completion.

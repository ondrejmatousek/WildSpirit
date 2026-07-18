---
name: testing-verification
description: Use after implementing application behavior. Select verification proportional to the risk of the change. Never claim success when required verification fails.
---

# Testing and Verification

## Rules

- Add tests only for new or changed behavior.
- Prefer unit tests for business rules.
- Use integration tests only when interaction with EF Core, routing, authorization or infrastructure is important.
- Keep tests deterministic.

## Verification

Choose the smallest verification that gives confidence.

### Small change

- Build affected project.
- Run relevant targeted tests if applicable.

### Functional change

- Build affected projects.
- Run relevant tests.
- Verify changed behavior.

### Schema or infrastructure

- Build solution.
- Run relevant tests.
- Verify migrations when schema changed.

### Release / Stage completion

- Run full build.
- Run full test suite.
- Start application if runtime verification is required.

## Reporting

Report:

- build status
- tests executed
- checks intentionally skipped
- failures and limitations

Never claim success when required verification failed.
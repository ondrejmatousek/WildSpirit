---
name: performance-observability
description: Use for query performance, caching, pagination, image delivery, logging, audit trails, health checks, diagnostics, or production-readiness work in the ASP.NET Core ecommerce application.
---

# Performance and Observability

## Performance

- Paginate all unbounded admin and catalog lists.
- Project read models; avoid loading full aggregates for list screens.
- Prevent N+1 queries.
- Add indexes based on actual filter, join, uniqueness, and ordering needs.
- Cache public reference data only with a clear invalidation strategy.
- Do not cache personalized, authorization-sensitive, cart, stock, or rapidly changing price data carelessly.
- Use responsive images, lazy loading, and explicit dimensions.
- Measure before introducing complex caching.

## Logging

Log meaningful events with structured properties:

- failures,
- authentication/security events,
- order and quote transitions,
- stock changes,
- payment events,
- admin changes.

Do not log secrets or excessive personal data. Prefer identifiers and correlation IDs.

## Audit

Audit who changed:

- product price and availability,
- stock,
- order state,
- inquiry and quote state,
- production state,
- permissions-sensitive content.

An audit record should include actor, timestamp UTC, action, entity type/id, and a safe summary.

## Health and diagnostics

- Add health checks for application and database.
- Startup verification should probe a representative route.
- Ensure production error pages do not expose stack traces.
- Use correlation IDs for troubleshooting.
- Make background/outbox failures observable and retryable without duplicate business effects.

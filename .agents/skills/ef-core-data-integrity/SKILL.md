---
name: ef-core-data-integrity
description: Use for Entity Framework Core entities, DbContext configuration, migrations, indexes, transactions, concurrency, queries, seed data, and SQL Server data integrity. Trigger whenever database schema or persistence behavior changes.
---

# EF Core and Data Integrity

## Model design

- Configure relationships, requiredness, maximum lengths, delete behavior, indexes, and unique constraints explicitly.
- Use `decimal` with explicit precision for money. Never use `double` for financial values.
- Store timestamps in UTC and name them with a `Utc` suffix.
- Use owned/value-object types only when they simplify the domain.
- Do not soft-delete every entity automatically. Preserve historical commercial records.
- Orders, quotes, inquiries, payments, and stock movements must retain immutable historical snapshots where required.

## Queries

- Use projection to DTOs for read screens.
- Avoid N+1 queries.
- Use `AsNoTracking()` for read-only queries.
- Use `Include` only when the complete graph is genuinely needed.
- Add pagination to potentially large lists.
- Inspect generated query shape when performance is uncertain.

## Migrations

1. Review the model diff before creating a migration.
2. Give migrations descriptive names.
3. Inspect generated migration code.
4. Ensure destructive changes are deliberate and documented.
5. Verify a database can be created from scratch.
6. Verify migration from the previous expected schema when feasible.
7. Never silently drop business data to make a migration pass.

## Concurrency and transactions

- Use transactions for operations that must succeed atomically.
- Use optimistic concurrency tokens for admin edits or stock rows when concurrent updates are possible.
- Translate concurrency failures into clear user-facing errors.
- Do not rely on an earlier read to guarantee stock at checkout.

## Seed data

- Development seed must be idempotent.
- Do not hard-code production credentials.
- Do not create duplicates on each startup.
- Keep test fixtures deterministic.

## Verification

Run migrations against a disposable or development database, then build, test, and start the application.

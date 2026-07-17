# DesignStore repository instructions

## Project purpose

This repository is a production-oriented ASP.NET Core MVC ecommerce application for premium design furniture, lighting, leather handbags, belts, and accessories.

## Mandatory business rules

- A concrete product or variant in `InStock` may be purchased.
- A concrete product or variant in `MadeToOrder` may only create a product-linked manufacturing inquiry.
- `Unavailable` and `Discontinued` items cannot be purchased or inquired about.
- Prevent overselling with database-level transactional or concurrency-safe logic.
- Orders, inquiries, and quotes store historical snapshots.
- Use `decimal` for money and explicit rounding/precision.
- Customer-visible information must never expose internal notes, costs, margins, or private files.

## Engineering expectations

- Follow existing `ARCHITECTURE.md`, `DATA_MODEL.md`, `IMPLEMENTATION_PLAN.md`, and `DECISIONS.md`.
- Keep controllers thin and business rules outside Web and Razor views.
- Use server-side validation and authorization.
- Use async APIs and propagate `CancellationToken`.
- Do not add major frameworks or production dependencies without a clear documented reason.
- Preserve existing conventions and keep changes within the approved stage.

## Verification required before declaring completion

- Restore dependencies when needed.
- Build the solution.
- Run relevant and full tests when practical.
- Apply and verify migrations when the schema changes.
- Start the actual application and verify a representative HTTP endpoint.
- Report failures honestly and do not proceed to a later stage automatically.

## Skills

Use the most relevant repository skills under `.agents/skills`. In particular:

- use `stage-gated-delivery` for every numbered implementation stage,
- use `testing-verification` before completion,
- use `ecommerce-inventory-orders` for cart, checkout, stock, and orders,
- use `inquiries-quotes-production` for made-to-order workflows,
- use `ef-core-data-integrity` for persistence and migrations,
- use `aspnet-security-files` for security, roles, privacy, and uploads.

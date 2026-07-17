---
name: inquiries-quotes-production
description: Use for made-to-order product inquiries, quotes, customer quote acceptance, conversion to orders, deposits, and production-job tracking. The inquiry must always be linked to an existing MadeToOrder product or variant.
---

# Inquiries, Quotes, and Production

## Inquiry eligibility

Allow an inquiry only when the concrete product or selected variant:

- exists,
- is published,
- has `AvailabilityStatus.MadeToOrder`,
- has `CanBeMadeToOrder = true`.

Reject stale or forged requests server-side. Never allow a generic custom-manufacturing inquiry through this workflow.

## Inquiry snapshot

Persist a historical snapshot of:

- product and variant identifiers,
- display names and SKU,
- source URL,
- current starting/base price,
- selected configuration,
- estimated production time,
- relevant product image reference.

Later catalog edits must not alter the inquiry.

## Quotes

- A quote is versioned or otherwise protected from silent changes after sending.
- Persist line items, taxes, shipping, discount, deposit, balance, currency, validity, and production estimate.
- Customer access links must be unguessable, revocable or expiring, and must not reveal unrelated customer data.
- Acceptance and rejection must be idempotent.
- Reject acceptance after expiry, cancellation, prior conversion, or incompatible status.
- Persist customer comments and status history.

## Conversion to order

- Convert the accepted quote exactly once.
- Use a transaction for quote state, inquiry state, and order creation.
- Snapshot all commercial values into the order.
- Do not re-price accepted quotes from the current catalog.
- Record the relationship among inquiry, quote, order, payment/deposit, and production job.

## Production visibility

Separate internal notes and files from customer-visible updates. Authorization must be enforced server-side, not only hidden in UI.

## Tests

Test forged eligibility, expiry, double acceptance, double conversion, concurrent conversion, deposit calculations, and privacy of internal production notes.

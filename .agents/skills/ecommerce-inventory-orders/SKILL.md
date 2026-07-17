---
name: ecommerce-inventory-orders
description: Use for cart, checkout, inventory, product availability, stock reservation, order creation, cancellation, refunds, or prevention of overselling in this design-product ecommerce project.
---

# E-commerce Inventory and Orders

## Core availability rule

- `InStock`: purchasable through cart and checkout.
- `MadeToOrder`: not directly purchasable; only a product-linked manufacturing inquiry is allowed.
- `Unavailable`: neither purchasable nor inquirable.
- `Discontinued`: neither purchasable nor inquirable; may remain public for gallery and SEO.

If availability is tracked by variant, the selected variant is the source of truth.

## Never oversell

- Revalidate publication, price, variant, availability, and stock during checkout.
- Perform final stock validation and mutation inside a database transaction.
- Use an atomic update or concurrency token so two buyers cannot acquire the same last unit.
- A cart is not proof of stock.
- Define clearly whether stock is reserved before payment. Do not invent reservations implicitly.
- On cancellation or failed order finalization, restore stock exactly once when business rules require it.
- Every stock mutation must be auditable through a stock movement.

## Order snapshots

Each order item must snapshot at least:

- product and variant name,
- SKU,
- selected configuration,
- image reference where appropriate,
- quantity,
- unit price,
- discounts,
- tax rate and tax amount,
- currency,
- exchange rate when relevant.

Historical orders must not change when catalog data changes.

## Money

- Use `decimal`.
- Apply explicit rounding rules at one defined boundary.
- Keep subtotal, discount, tax, shipping, and total independently explainable.
- Never trust prices posted from the browser.

## State transitions

- Validate allowed order-state transitions centrally.
- Make transitions idempotent where external callbacks or retries can occur.
- Persist status history.
- Side effects such as email must not leave the order transaction inconsistent.

## Tests required

Cover the last-item race, zero stock, variant stock, invalid price, duplicate execution, cancellation restoration, and automatic switch to `MadeToOrder` or `Unavailable`.

---
name: product-catalog-admin
description: Use for product, variant, category, collection, material, color, product option, image, publication, availability, and admin catalog screens in the DesignStore ecommerce application.
---

# Product Catalog Administration

## Admin UX

- Use dedicated view models for create, edit, list, and detail screens.
- Split product editing into logical sections or tabs.
- Preserve entered data and validation messages after failure.
- Use pagination, search, filters, and clear status badges.
- Confirm destructive actions.
- Prevent accidental data loss when editing translations, variants, and images.

## Catalog integrity

- SKU must be unique according to the documented scope.
- Localized slug must be unique within language and entity route scope.
- Prevent category hierarchy cycles.
- Do not delete a category that still owns products without an explicit migration strategy.
- Ensure product-level and variant-level stock rules cannot contradict each other.
- Validate `MadeToOrder` requires `CanBeMadeToOrder`.
- A discontinued item cannot be represented as available stock.

## Images

- Validate extension, MIME type, content signature when available, size, and image decoding.
- Generate safe storage names; do not use the original name as a path.
- Maintain one main image per product.
- Keep deterministic ordering.
- Clean up files safely when an upload fails or a record is removed.
- Never expose arbitrary server file paths.

## Public catalog

- Show only published content.
- Resolve current language with documented fallback.
- Resolve price and availability from the selected variant when variants exist.
- Do not display active purchase controls before the relevant workflow exists.
- Keep catalog query models optimized and paginated.

## Scope discipline

Do not add a complex product-rule engine unless the requirement explicitly needs dependent combinations. Prefer a simple option model that can be extended later.

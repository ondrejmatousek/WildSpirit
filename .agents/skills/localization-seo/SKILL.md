---
name: localization-seo
description: Use for Czech, English, and German localization, localized slugs and routes, currency presentation, metadata, canonical URLs, hreflang, sitemap, structured data, and ecommerce SEO.
---

# Localization and SEO

## Localization

- Supported languages are Czech, English, and German; Czech is the default unless project documentation says otherwise.
- UI text must come from localization resources.
- Domain content uses translation records.
- Use a documented fallback to Czech when optional translations are missing.
- Do not crash or emit empty critical labels because a translation is absent.
- Preserve the selected culture across navigation and form validation.
- Format dates, numbers, and currency using the active culture while retaining invariant internal values.

## Localized slugs

- Normalize safely and remove unsupported characters.
- Enforce uniqueness within language and route/entity scope.
- Keep historical slugs for permanent redirects when implemented.
- Avoid two live URLs for the same language/content.
- Validate manually edited slugs.

## SEO

- Generate title and description fallbacks from translated content.
- Use canonical URLs consistently.
- Emit hreflang for valid language alternatives.
- Keep sitemap limited to public, indexable canonical URLs.
- Use `noindex` deliberately for non-public or duplicate pages.
- Add accurate JSON-LD for Organization, Product, Offer, and BreadcrumbList.
- Structured data must reflect real price, currency, availability, and URL.
- Do not mark `MadeToOrder` as ordinary in-stock inventory.
- Use meaningful image alt text and dimensions.
- Preserve discontinued pages when useful, but recommend live alternatives.

## Testing

Test culture fallback, localized route generation, slug collisions, canonical output, and structured-data availability mapping.

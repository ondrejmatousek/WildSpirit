---
name: razor-premium-ui
description: Use when creating or refining Razor MVC pages, responsive layouts, product cards, admin screens, forms, accessibility, or the premium visual design of the DesignStore ecommerce site.
---

# Premium Razor UI

## Visual direction

Create a restrained premium European design aesthetic:

- generous whitespace,
- strong typography,
- large product photography,
- neutral natural palette,
- subtle motion,
- clear hierarchy,
- minimal decoration.

Avoid generic template appearance, excessive cards, loud gradients, unnecessary shadows, icon clutter, and animation that delays interaction.

## Razor and CSS

- Prefer semantic HTML and server-rendered Razor.
- Use lightweight JavaScript only for genuine interaction.
- Keep business and database logic out of views.
- Reuse partials and view components for repeated meaningful UI.
- Use CSS custom properties for typography, spacing, colors, radius, widths, and motion.
- Ensure responsive behavior at content-driven breakpoints.
- Avoid adding a heavy UI dependency for one component.

## Forms

- Every input has a label.
- Show field errors near fields and a useful validation summary.
- Preserve values on validation failure.
- Clearly distinguish required and optional fields.
- Make destructive and irreversible actions explicit.
- Provide visible loading and disabled states for submitted actions where double submission is possible.

## Accessibility

- Maintain heading order and landmarks.
- Ensure keyboard access and visible focus.
- Meet reasonable contrast.
- Use alt text based on content purpose.
- Ensure modals trap focus and restore it.
- Do not encode meaning by color alone.
- Respect reduced-motion preferences.

## E-commerce details

- Availability, price, selected variant, delivery estimate, and primary action must remain obvious.
- Never show a purchase action for `MadeToOrder`, `Unavailable`, or `Discontinued`.
- Mobile product pages must keep selection and validation understandable without hiding critical information.

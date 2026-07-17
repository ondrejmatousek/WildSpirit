---
name: dotnet-clean-architecture
description: Use when designing, extending, or reviewing an ASP.NET Core solution with Domain, Application, Infrastructure, Web, dependency injection, DTOs, services, and clean dependency boundaries. Do not use for tiny isolated edits that do not affect architecture.
---

# .NET Clean Architecture

Apply these rules when changing the solution architecture or adding a feature that crosses layers.

## Boundaries

- Domain contains entities, value objects, enums, domain rules, and domain exceptions.
- Domain must not reference Application, Infrastructure, or Web.
- Application contains use-case services, DTOs, interfaces, validation, and orchestration.
- Infrastructure implements persistence, files, email, payments, and other external concerns.
- Web handles HTTP, model binding, authorization, views, and composition.
- Tests may reference the layers they test.

## Implementation rules

- Keep controllers thin. They validate request shape, call an application service, and map the result to a response or view.
- Do not put business rules in controllers, Razor views, EF configurations, or JavaScript.
- Prefer explicit use-case services over generic repositories that only wrap `DbSet`.
- Introduce abstractions only at real boundaries or when needed for testing.
- Use DTOs and dedicated input/view models. Do not bind EF entities directly to public forms.
- Use `async` APIs and propagate `CancellationToken`.
- Enable nullable reference types and resolve warnings rather than suppressing them broadly.
- Keep methods and classes focused. Split large services by use case.
- Do not add MediatR, CQRS, event buses, or mapping libraries unless the repository already uses them or the change clearly benefits from them.

## Before coding

1. Read `AGENTS.md`, `ARCHITECTURE.md`, `DATA_MODEL.md`, `DECISIONS.md`, and the relevant feature code.
2. Identify which layer owns each rule and side effect.
3. Preserve existing conventions unless they are demonstrably harmful.
4. Record material architectural decisions in `DECISIONS.md`.

## Completion

- Run restore, build, and relevant tests.
- Verify the actual application can start when the change affects startup, configuration, routing, DI, EF Core, or Razor compilation.
- Report changed boundaries and any remaining compromise.

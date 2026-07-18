---
name: dotnet-clean-architecture
description: Use when implementing features that span multiple layers or when changing architecture. Do not use for isolated UI, CSS, or text changes.
---

# .NET Clean Architecture

## Layer ownership

- Domain: entities, value objects, business rules.
- Application: use cases, DTOs, validation, orchestration.
- Infrastructure: EF Core, files, email, external services.
- Web: controllers, Razor, model binding, authorization.

## Rules

- Keep controllers thin.
- Keep business logic out of Web and Razor.
- Use DTOs/ViewModels for public input.
- Use async APIs with `CancellationToken`.
- Respect existing architecture and conventions.
- Introduce new abstractions only when they provide clear value.
- Do not introduce MediatR, CQRS, mapping libraries or other architectural frameworks without explicit approval.

## Verification

- Build affected projects.
- Run relevant tests.
- Verify startup only if DI, routing, EF Core or configuration changed.

## Report

Mention any architectural decisions or boundary changes.
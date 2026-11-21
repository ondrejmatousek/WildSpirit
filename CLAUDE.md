# WildSpirit (DesignStore)

Personal application. Development workflow is defined centrally in the
app-development skill (C:\AI-Harness\skills\app-development\SKILL.md).

- Start development tasks with `/app-development <task>`.
- Shared rules: C:\AI-Harness\rules (coding.md, security.md, analysis.md)
  and quality gates: C:\AI-Harness\gates\quality-gates.md.
- Project configuration (build/tests/review): C:\AI-Harness\projects.
- Ask before destructive operations, deployment, fundamental architecture
  changes, commits and pushes.
- Project rules: see AGENTS.md, ARCHITECTURE.md and DATA_MODEL.md.

## Project notes
- Technology: ASP.NET Core 10 MVC, EF Core + PostgreSQL 17 (Npgsql), ASP.NET Identity, xUnit (+ Playwright E2E)
- Build / run: `dotnet build DesignStore.slnx`; tests `dotnet test` (DB tests use local PostgreSQL localhost:5432 with password from %APPDATA%\postgresql\pgpass.conf, or `DESIGNSTORE_TEST_CONNECTION`)

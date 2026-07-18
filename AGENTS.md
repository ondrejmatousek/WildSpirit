# \# DesignStore

# 

# \## Project

# 

# Production ASP.NET Core MVC ecommerce application.

# 

# \## Rules

# 

# \- Respect ARCHITECTURE.md and DATA\_MODEL.md when relevant.

# \- Keep controllers thin.

# \- Keep business logic outside Web and Razor.

# \- Use async APIs and CancellationToken.

# \- Use decimal for money.

# \- Prevent overselling.

# \- Preserve historical snapshots.

# \- Never expose internal information.

# 

# \## Scope

# 

# \- Modify only what the task requires.

# \- Avoid unrelated refactoring.

# \- Do not continue to another stage automatically.

# \- Reuse existing architecture and services.

# 

# \## Verification

# 

# Use verification proportional to the change.

# 

# Small change:

# \- Build affected project only.

# 

# Functional change:

# \- Build + relevant tests.

# 

# Schema/security/stage completion:

# \- Build solution.

# \- Run tests.

# \- Verify migrations if needed.

# 

# \## Skills

# 

# Use only the skills relevant to the current task.


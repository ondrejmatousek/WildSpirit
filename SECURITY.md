# Security and privacy checklist

## Runtime security

- Production uses HTTPS, HSTS and security headers from `SecurityHeadersMiddleware`.
- Content Security Policy is enabled outside Development and allows only self-hosted scripts, self-hosted images, Google Fonts stylesheets and Google Fonts font files.
- Administration is protected by ASP.NET Core Identity roles and policies.
- Authentication cookies are HttpOnly, SameSite=Lax and marked as essential because login and cart flows require them.
- Google/Facebook credentials must be configured through user secrets, environment variables or the hosting secret store. They must never be committed.
- Test payment is development-only and must not be enabled in Production.

## Uploads

- Uploaded files are stored outside `wwwroot`.
- Product images are validated server-side, decoded, metadata-stripped and re-encoded before publication.
- File storage root must not point into source control or a public static folder.

## GDPR and retention

- Customers can download a JSON export from `Můj účet`.
- Customers can submit a deletion/anonymization request from `Můj účet`.
- Deletion requests are queued for manual review through the outbox because orders, invoices, payments and legal records can have statutory retention obligations.
- Historical commercial documents are not physically deleted by ordinary account actions; personal data should be anonymized only after legal retention checks.
- Newsletter tokens are stored only as hashes.

## Logging

- Do not log passwords, OAuth secrets, raw access tokens, payment secrets or full uploaded file contents.
- Prefer IDs, correlation IDs and high-level status values.
- If production logging is expanded, add redaction for e-mail, phone and address fields unless explicitly needed for a support case.

## Deployment checklist

- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Set the production PostgreSQL connection string through secrets (environment variable or env file outside the repository).
- Set `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret`, Facebook values and any future provider secrets through the hosting secret store.
- Configure a real e-mail provider before enabling customer-facing e-mail delivery.
- Configure a production file/object storage location.
- Run `dotnet ef database update` against the target database before starting the web container.
- Verify `/health`, `/robots.txt`, `/sitemap.xml`, login, catalog detail and checkout smoke paths.

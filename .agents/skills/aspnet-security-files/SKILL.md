---
name: aspnet-security-files
description: Use for ASP.NET Core authentication, authorization, Identity, admin roles, public forms, file uploads, cookies, secrets, security headers, rate limiting, anti-forgery, privacy, or GDPR-related implementation.
---

# ASP.NET Core Security and File Handling

## Authorization

- Enforce authorization on endpoints and use-case services where sensitive operations occur.
- Hiding UI controls is not authorization.
- Use least privilege for Administrator, Sales, Production, ContentManager, and Customer.
- Prevent insecure direct object reference by checking resource ownership or staff permission.

## Forms and browser security

- Use anti-forgery protection on state-changing browser forms.
- Encode output by default.
- Sanitize only the limited administrator HTML fields that require HTML.
- Configure secure, HttpOnly, and appropriate SameSite cookies.
- Use HTTPS redirection and suitable security headers.
- Add rate limiting and anti-spam protection to public forms.
- Return generic authentication and reset-password responses where account enumeration is a risk.

## Uploads

- Allow only documented types and limits.
- Validate extension, declared MIME type, and actual parsability/signature.
- Generate random storage names.
- Store outside executable application paths.
- Never concatenate user input into a path.
- Prevent path traversal and overwrite.
- Scan or quarantine files when infrastructure supports it.
- Serve downloads through authorized endpoints when files are private.
- Remove metadata from public images where appropriate.
- Do not render uploaded SVG or HTML unless explicitly sanitized and required.

## Secrets and logs

- Use user secrets, environment variables, or a production secret store.
- Never commit real credentials.
- Do not log passwords, tokens, payment data, or unnecessary personal data.
- Mask sensitive identifiers in diagnostic output.

## Privacy

- Collect only required data.
- Record consent purpose, version, time, and withdrawal where needed.
- Do not load analytics or marketing scripts before consent.
- Preserve commercial records when legally required while separating account deletion from record retention.

## Completion

Include negative authorization and upload-security tests, not only happy paths.

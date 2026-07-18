namespace DesignStore.Web.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.TryAdd("X-Content-Type-Options", "nosniff");
        headers.TryAdd("X-Frame-Options", "DENY");
        headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
        headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
        headers.TryAdd("Cross-Origin-Resource-Policy", "same-origin");
        headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");

        if (!environment.IsDevelopment())
        {
            headers.TryAdd("Content-Security-Policy",
                "default-src 'self'; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "frame-ancestors 'none'; " +
                "img-src 'self' data: blob:; " +
                "font-src 'self' https://fonts.gstatic.com; " +
                "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
                "script-src 'self'; " +
                "form-action 'self'; " +
                "connect-src 'self'; " +
                "upgrade-insecure-requests");
        }

        await next(context);
    }
}

namespace LibraryConnect.Api.Middleware;

/// <summary>
/// Adds the security response headers required by section 6.4. The SPAs are served by Nginx, which
/// sets its own CSP for the HTML; the API only ever returns JSON and file streams, so its policy can
/// be as strict as possible.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-XSS-Protection"] = "0";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        // Swagger UI is real HTML served by this same app and needs its own scripts and styles;
        // every other response is JSON or a file stream and gets the strictest possible policy.
        headers["Content-Security-Policy"] = context.Request.Path.StartsWithSegments("/swagger")
            ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'"
            : "default-src 'none'; frame-ancestors 'none'; sandbox";

        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        return _next(context);
    }
}

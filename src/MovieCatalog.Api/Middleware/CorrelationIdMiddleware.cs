namespace MovieCatalog.Api.Middleware;

/// <summary>
/// Ensures every request carries a correlation id: reuses the inbound
/// "X-Correlation-Id" header when present, otherwise generates one. The id is
/// stored in HttpContext.Items so controllers/services can read it, and echoed
/// back in the response header for client-side tracing.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public static class HttpContextCorrelationExtensions
{
    public static string GetCorrelationId(this HttpContext context)
        => context.Items.TryGetValue(CorrelationIdMiddleware.ItemKey, out var value) && value is string id
            ? id
            : Guid.NewGuid().ToString();
}

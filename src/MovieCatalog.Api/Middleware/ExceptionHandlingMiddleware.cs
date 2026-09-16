using System.Net;
using System.Text.Json;
using FluentValidation;
using MovieCatalog.Domain.Exceptions;

namespace MovieCatalog.Api.Middleware;

/// <summary>
/// Centralizes error-to-HTTP-response mapping so controllers stay free of try/catch:
/// FluentValidation.ValidationException and BusinessRuleValidationException -> 400,
/// NotFoundException -> 404, anything else -> 500 with a correlation id (also present
/// in the log entry) for support/tracing.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.GetCorrelationId();
        context.Response.ContentType = "application/json";

        object body;

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning(exception, "Validation error. CorrelationId={CorrelationId}", correlationId);
                body = new
                {
                    status = 400,
                    title = "Erro de validação.",
                    correlationId,
                    errors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                };
                break;

            case BusinessRuleValidationException businessRuleException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                _logger.LogWarning(exception, "Business rule violation. CorrelationId={CorrelationId}", correlationId);
                body = new { status = 400, title = businessRuleException.Message, correlationId };
                break;

            case NotFoundException notFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                _logger.LogWarning(exception, "Resource not found. CorrelationId={CorrelationId}", correlationId);
                body = new { status = 404, title = notFoundException.Message, correlationId };
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                _logger.LogError(exception, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);
                body = new
                {
                    status = 500,
                    title = "Ocorreu um erro interno inesperado.",
                    correlationId
                };
                break;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }
}

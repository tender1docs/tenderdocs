using System.Text.Json;
using TenderDocs.Application.Common.Exceptions;

namespace TenderDocs.Api.Middleware;

/// <summary>Translates domain/application exceptions into RFC7807 ProblemDetails responses.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        => (_next, _logger) = (next, logger);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(ctx, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext ctx, Exception ex)
    {
        var (status, title, errors) = ex switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed", ve.Errors),
            NotFoundException => (StatusCodes.Status404NotFound, ex.Message, null),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, "Forbidden", null),
            ConflictException => (StatusCodes.Status409Conflict, ex.Message, null),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Unhandled exception");
        else
            _logger.LogWarning("Handled {Type}: {Message}", ex.GetType().Name, ex.Message);

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.io/{status}",
            ["title"] = title,
            ["status"] = status,
            ["traceId"] = ctx.TraceIdentifier,
        };
        if (errors is not null) payload["errors"] = errors;

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}

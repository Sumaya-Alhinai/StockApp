using System.Text.Json;
using StockApp.Application.Common.Exceptions;

namespace StockApp.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, code, message, errors) = ex switch
        {
            ValidationException v => (400, "VALIDATION_ERROR", v.Message, v.Errors),
            InvalidCredentialsException => (401, "INVALID_CREDENTIALS", ex.Message, null),
            NotFoundException => (404, "NOT_FOUND", ex.Message, null),
            ConflictException c => (409, c.Code, c.Message, null),
            _ => (500, "INTERNAL_ERROR", "An unexpected error occurred.", (IDictionary<string, string[]>?)null)
        };

        if (status == 500)
            _logger.LogError(ex, "Unhandled exception");

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(new
        {
            code,
            message,
            errors
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(payload);
    }
}
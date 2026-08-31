using System.Text.Json;
using FluentValidation;
using RealEstate.Core.Exceptions;

namespace RealEstate.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object body;

        switch (exception)
        {
            case ValidationAppException validationEx:
                context.Response.StatusCode = validationEx.StatusCode;
                body = new { message = validationEx.Message, errors = validationEx.Errors };
                break;

            case FluentValidation.ValidationException fluentEx:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                body = new
                {
                    message = "One or more validation errors occurred.",
                    errors = fluentEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                };
                break;

            case AppException appEx:
                context.Response.StatusCode = appEx.StatusCode;
                body = new { message = appEx.Message };
                logger.LogWarning(appEx, "Handled application exception: {Message}", appEx.Message);
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                body = new { message = "An unexpected error occurred." };
                logger.LogError(exception, "Unhandled exception");
                break;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}

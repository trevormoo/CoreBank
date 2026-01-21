using System.Net;
using System.Text.Json;
using CoreBank.Application.Common.Exceptions;
using CoreBank.Domain.Exceptions;

namespace CoreBank.Api.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case ValidationException validationEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = "Validation failed";
                errorResponse.Errors = validationEx.Errors;
                _logger.LogWarning("Validation error: {Errors}", validationEx.Errors);
                break;

            case NotFoundException notFoundEx:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Message = notFoundEx.Message;
                _logger.LogWarning("Not found: {Message}", notFoundEx.Message);
                break;

            case ForbiddenAccessException forbiddenEx:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                errorResponse.Message = forbiddenEx.Message;
                _logger.LogWarning("Forbidden access: {Message}", forbiddenEx.Message);
                break;

            case DomainException domainEx:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Message = domainEx.Message;
                _logger.LogWarning("Domain error: {Message}", domainEx.Message);
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Message = "Unauthorized";
                _logger.LogWarning("Unauthorized access attempt");
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Message = "An unexpected error occurred";
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                break;
        }

        var result = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(result);
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public IDictionary<string, string[]>? Errors { get; set; }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}

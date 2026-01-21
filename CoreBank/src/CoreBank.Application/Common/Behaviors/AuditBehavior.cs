using System.Diagnostics;
using CoreBank.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreBank.Application.Common.Behaviors;

public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<AuditBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public AuditBehavior(
        ILogger<AuditBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId ?? "Anonymous";

        // Only audit commands (writes), not queries (reads)
        var isCommand = requestName.EndsWith("Command");

        if (!isCommand)
        {
            return await next();
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "Command {CommandName} executed by {UserId} in {ElapsedMilliseconds}ms",
                requestName,
                userId,
                stopwatch.ElapsedMilliseconds);

            // Log successful command execution
            await _auditService.LogAsync(
                userId,
                requestName,
                "Command",
                GetEntityId(request),
                null,
                new { Request = SanitizeRequest(request), Success = true, Duration = stopwatch.ElapsedMilliseconds },
                cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Command {CommandName} failed for {UserId} after {ElapsedMilliseconds}ms",
                requestName,
                userId,
                stopwatch.ElapsedMilliseconds);

            // Log failed command execution
            await _auditService.LogAsync(
                userId,
                requestName,
                "Command",
                GetEntityId(request),
                null,
                new { Request = SanitizeRequest(request), Success = false, Error = ex.Message, Duration = stopwatch.ElapsedMilliseconds },
                cancellationToken);

            throw;
        }
    }

    private static string GetEntityId(TRequest request)
    {
        // Try to extract entity ID from common property names
        var type = request.GetType();

        var idProperties = new[] { "Id", "AccountId", "UserId", "TransactionId", "DocumentId" };

        foreach (var propName in idProperties)
        {
            var prop = type.GetProperty(propName);
            if (prop != null)
            {
                var value = prop.GetValue(request);
                if (value != null)
                    return value.ToString()!;
            }
        }

        return "N/A";
    }

    private static object SanitizeRequest(TRequest request)
    {
        // Create a sanitized version that excludes sensitive data
        var type = request.GetType();
        var sanitized = new Dictionary<string, object?>();

        foreach (var prop in type.GetProperties())
        {
            var name = prop.Name.ToLower();

            // Skip sensitive fields
            if (name.Contains("password") ||
                name.Contains("secret") ||
                name.Contains("token") ||
                name.Contains("key"))
            {
                sanitized[prop.Name] = "[REDACTED]";
            }
            else
            {
                sanitized[prop.Name] = prop.GetValue(request);
            }
        }

        return sanitized;
    }
}

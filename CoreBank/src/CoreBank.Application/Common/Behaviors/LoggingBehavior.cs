using System.Diagnostics;
using CoreBank.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CoreBank.Application.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId ?? "Anonymous";

        _logger.LogInformation(
            "CoreBank Request: {Name} {@UserId} {@Request}",
            requestName, userId, request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            _logger.LogInformation(
                "CoreBank Request Completed: {Name} {@UserId} - {ElapsedMilliseconds}ms",
                requestName, userId, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "CoreBank Request Failed: {Name} {@UserId} - {ElapsedMilliseconds}ms - {Error}",
                requestName, userId, stopwatch.ElapsedMilliseconds, ex.Message);

            throw;
        }
    }
}

using System.Diagnostics;
using LibraryConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LibraryConnect.Application.Common.Behaviours;

/// <summary>Structured timing log for every use case, used to spot slow handlers in production.</summary>
public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 1000;

    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;
    private readonly ICurrentUser _currentUser;

    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger, ICurrentUser currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
            {
                _logger.LogWarning("Use case {UseCase} took {Elapsed} ms for user {User}",
                    name, stopwatch.ElapsedMilliseconds, _currentUser.Username ?? "anonymous");
            }
            else
            {
                _logger.LogDebug("Use case {UseCase} completed in {Elapsed} ms", name, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Use case {UseCase} failed after {Elapsed} ms for user {User}",
                name, stopwatch.ElapsedMilliseconds, _currentUser.Username ?? "anonymous");
            throw;
        }
    }
}

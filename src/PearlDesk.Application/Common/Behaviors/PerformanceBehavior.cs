using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PearlDesk.Application.Common.Behaviors;

public class PerformanceBehavior<TRequest, TResponse>(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private const int WarningThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > WarningThresholdMs)
        {
            logger.LogWarning(
                "[Performance] {RequestName} took {ElapsedMs}ms (threshold: {Threshold}ms)",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                WarningThresholdMs);
        }

        return response;
    }
}


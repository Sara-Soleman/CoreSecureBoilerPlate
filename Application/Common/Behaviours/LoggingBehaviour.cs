using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Application.Common.Behaviours
{
    public class LoggingBehaviour<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogInformation("Request processing is in progress: {Name}| Data: {@Request}", requestName,request);

            var timer = Stopwatch.StartNew();
            var response = await next();
            timer.Stop();

            var elapsedMilliseconds = timer.ElapsedMilliseconds;

            // If the order takes too long (search for Bottleneck)
            if (elapsedMilliseconds > 500)
            {
                logger.LogWarning("Slow performance alert: {Name} took ({ElapsedMilliseconds} ms)| Payload: {@Request}",
                    requestName, elapsedMilliseconds,request);
            }

            logger.LogInformation("Finished processing request: {Name}", requestName);
            return response;
        }
    }
}

using Serilog.Context;

namespace CoreSecureBoilerPlate.Middleware
{
    public class CorrelationIdMiddleware(RequestDelegate next)
    {
        private const string CorrelationIdHeaderKey = "X-Correlation-ID";

        public async Task InvokeAsync(HttpContext context)
        {
            //Retrieve the Correlation ID from the Header if available, or generate a new one.
            if (!context.Request.Headers.TryGetValue(CorrelationIdHeaderKey, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Return the ID in the Response so that the Frontend can track it in case of a problem.
            context.Response.Headers.Append(CorrelationIdHeaderKey, correlationId);

            // The identifier is embedded in the context of all logs throughout the request period.
            using (LogContext.PushProperty("CorrelationId", correlationId.ToString()))
            {
                await next(context);
            }
        }
    }
}

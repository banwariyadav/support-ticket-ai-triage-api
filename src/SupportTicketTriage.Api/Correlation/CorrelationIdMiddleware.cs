using SupportTicketTriage.Application.Abstractions;

namespace SupportTicketTriage.Api.Correlation;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = Guid.TryParse(incoming, out var parsed)
            ? parsed.ToString()
            : Guid.NewGuid().ToString();

        accessor.CorrelationId = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await _next(context);
        }
    }
}

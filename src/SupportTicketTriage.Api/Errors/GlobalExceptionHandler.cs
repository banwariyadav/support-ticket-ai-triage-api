using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SupportTicketTriage.Application.Exceptions;

namespace SupportTicketTriage.Api.Errors;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            EventPublishException => (
                StatusCodes.Status503ServiceUnavailable,
                "Event publication failed",
                "The ticket was saved, but the TicketCreated event could not be confirmed."),
            AiAnalysisException => (
                StatusCodes.Status500InternalServerError,
                "Ticket analysis failed",
                "The ticket could not be classified."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "An unexpected error occurred while processing the request.")
        };

        if (exception is EventPublishException publishException)
        {
            _logger.LogError(
                exception,
                "Unhandled broker failure. TicketId={TicketId} EventId={EventId} CorrelationId={CorrelationId} ExceptionCategory={ExceptionCategory}",
                publishException.TicketId,
                publishException.EventId,
                publishException.CorrelationId,
                exception.GetType().Name);
        }
        else if (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Unhandled exception. ExceptionCategory={ExceptionCategory}",
                exception.GetType().Name);
        }

        if (exception is OperationCanceledException)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.com/{status}",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}

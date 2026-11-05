namespace SupportTicketTriage.Application.Exceptions;

public sealed class EventPublishException : Exception
{
    public EventPublishException(Guid ticketId, Guid eventId, string correlationId, Exception innerException)
        : base("TicketCreated event could not be confirmed after the ticket was saved.", innerException)
    {
        TicketId = ticketId;
        EventId = eventId;
        CorrelationId = correlationId;
    }

    public Guid TicketId { get; }
    public Guid EventId { get; }
    public string CorrelationId { get; }
}

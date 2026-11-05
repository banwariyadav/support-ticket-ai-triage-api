namespace SupportTicketTriage.Infrastructure.Events;

public sealed record TicketCreatedEventEnvelope(
    Guid EventId,
    string EventType,
    string SchemaVersion,
    string CorrelationId,
    DateTime OccurredAtUtc,
    TicketCreatedEventData Data);

public sealed record TicketCreatedEventData(
    Guid TicketId,
    string Category,
    string Priority);

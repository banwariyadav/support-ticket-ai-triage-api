namespace SupportTicketTriage.Domain.Events;

public sealed record TicketCreatedEvent
{
    public required Guid EventId { get; init; }
    public required Guid TicketId { get; init; }
    public required string Category { get; init; }
    public required string Priority { get; init; }
    public required string CorrelationId { get; init; }
    public required DateTime OccurredAtUtc { get; init; }
}

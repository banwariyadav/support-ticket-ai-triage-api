using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Domain.Events;

namespace SupportTicketTriage.Infrastructure.Events;

public sealed class InMemoryEventPublisher : IEventPublisher
{
    private readonly ConcurrentQueue<TicketCreatedEvent> _published = new();
    private readonly ILogger<InMemoryEventPublisher> _logger;

    public InMemoryEventPublisher(ILogger<InMemoryEventPublisher> logger)
    {
        _logger = logger;
    }

    public IReadOnlyCollection<TicketCreatedEvent> Published => _published.ToArray();

    public Task PublishAsync(TicketCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _published.Enqueue(domainEvent);
        _logger.LogInformation(
            "Published TicketCreated in-memory. TicketId={TicketId} EventId={EventId} CorrelationId={CorrelationId} PublishOutcome={PublishOutcome}",
            domainEvent.TicketId,
            domainEvent.EventId,
            domainEvent.CorrelationId,
            "InMemory");
        return Task.CompletedTask;
    }
}

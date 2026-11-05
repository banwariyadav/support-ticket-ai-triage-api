using SupportTicketTriage.Domain.Events;

namespace SupportTicketTriage.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(TicketCreatedEvent domainEvent, CancellationToken cancellationToken = default);
}

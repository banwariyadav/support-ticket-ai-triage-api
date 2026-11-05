using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Domain.Events;

namespace SupportTicketTriage.Tests.Fakes;

public sealed class FakeEventPublisher : IEventPublisher
{
    public List<TicketCreatedEvent> Events { get; } = [];
    public Exception? Failure { get; set; }

    public Task PublishAsync(TicketCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Failure is not null)
        {
            throw Failure;
        }

        Events.Add(domainEvent);
        return Task.CompletedTask;
    }
}

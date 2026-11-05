using Azure.Messaging.ServiceBus;

namespace SupportTicketTriage.Infrastructure.Events;

/// <summary>
/// Testable seam around <see cref="ServiceBusSender"/>. Automated tests replace this adapter;
/// they never call Azure.
/// </summary>
public interface IServiceBusMessageSender
{
    Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default);
}

using Azure.Messaging.ServiceBus;

namespace SupportTicketTriage.Infrastructure.Events;

public sealed class ServiceBusSenderAdapter : IServiceBusMessageSender
{
    private readonly ServiceBusSender _sender;

    public ServiceBusSenderAdapter(ServiceBusSender sender)
    {
        _sender = sender;
    }

    public Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default) =>
        _sender.SendMessageAsync(message, cancellationToken);
}

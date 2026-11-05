using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SupportTicketTriage.Domain;
using SupportTicketTriage.Domain.Events;
using SupportTicketTriage.Infrastructure.Events;
using SupportTicketTriage.Infrastructure.Options;

namespace SupportTicketTriage.Tests.Events;

public sealed class AzureServiceBusEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_MapsStableMessageMetadata_WithoutCallingAzure()
    {
        var sender = new CapturingServiceBusSender();
        var publisher = new AzureServiceBusEventPublisher(
            sender,
            Options.Create(new AzureServiceBusOptions
            {
                FullyQualifiedNamespace = "demo.servicebus.windows.net",
                QueueName = "ticket-created"
            }),
            NullLogger<AzureServiceBusEventPublisher>.Instance);

        var domainEvent = new TicketCreatedEvent
        {
            EventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            TicketId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Category = TicketCategories.Billing,
            Priority = TicketPriorities.High,
            CorrelationId = "cccccccc-cccc-cccc-cccc-cccccccccccc",
            OccurredAtUtc = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc)
        };

        await publisher.PublishAsync(domainEvent);

        Assert.NotNull(sender.Last);
        Assert.Equal(domainEvent.EventId.ToString(), sender.Last!.MessageId);
        Assert.Equal("TicketCreated", sender.Last.Subject);
        Assert.Equal("application/json", sender.Last.ContentType);
        Assert.Equal(domainEvent.CorrelationId, sender.Last.CorrelationId);
        Assert.Equal("1.0", sender.Last.ApplicationProperties["schemaVersion"]);

        var body = sender.Last.Body.ToString();
        Assert.Contains("\"schemaVersion\":\"1.0\"", body, StringComparison.Ordinal);
        Assert.Contains(domainEvent.TicketId.ToString(), body, StringComparison.Ordinal);
        Assert.DoesNotContain("email", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CapturingServiceBusSender : IServiceBusMessageSender
    {
        public ServiceBusMessage? Last { get; private set; }

        public Task SendMessageAsync(ServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            Last = message;
            return Task.CompletedTask;
        }
    }
}

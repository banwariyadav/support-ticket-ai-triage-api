using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Domain.Events;
using SupportTicketTriage.Infrastructure.Options;

namespace SupportTicketTriage.Infrastructure.Events;

public sealed class AzureServiceBusEventPublisher : IEventPublisher
{
    public static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(30);

    private readonly IServiceBusMessageSender _sender;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger<AzureServiceBusEventPublisher> _logger;

    public AzureServiceBusEventPublisher(
        IServiceBusMessageSender sender,
        IOptions<AzureServiceBusOptions> options,
        ILogger<AzureServiceBusEventPublisher> logger)
    {
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(TicketCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var body = EventEnvelopeSerializer.Serialize(domainEvent);
        var message = new ServiceBusMessage(body)
        {
            MessageId = domainEvent.EventId.ToString(),
            Subject = EventEnvelopeSerializer.EventType,
            ContentType = "application/json",
            CorrelationId = domainEvent.CorrelationId
        };
        message.ApplicationProperties["schemaVersion"] = EventEnvelopeSerializer.SchemaVersion;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(SendTimeout);

        await _sender.SendMessageAsync(message, timeoutCts.Token);

        _logger.LogInformation(
            "Published TicketCreated to Azure Service Bus. TicketId={TicketId} EventId={EventId} CorrelationId={CorrelationId} QueueName={QueueName} Namespace={Namespace} Authentication={Authentication}",
            domainEvent.TicketId,
            domainEvent.EventId,
            domainEvent.CorrelationId,
            _options.QueueName,
            DescribeNamespace(_options),
            _options.UsesConnectionString ? "ConnectionString" : "DefaultAzureCredential");
    }

    private static string DescribeNamespace(AzureServiceBusOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
        {
            return options.FullyQualifiedNamespace;
        }

        if (!options.UsesConnectionString)
        {
            return string.Empty;
        }

        try
        {
            return ServiceBusConnectionStringProperties.Parse(options.ConnectionString).FullyQualifiedNamespace;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return string.Empty;
        }
    }
}

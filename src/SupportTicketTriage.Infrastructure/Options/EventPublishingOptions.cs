namespace SupportTicketTriage.Infrastructure.Options;

public sealed class EventPublishingOptions
{
    public const string SectionName = "EventPublishing";
    public const string InMemory = "InMemory";
    public const string AzureServiceBus = "AzureServiceBus";

    public string Provider { get; set; } = AzureServiceBus;
}

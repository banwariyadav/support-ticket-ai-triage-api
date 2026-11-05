using System.Text.Json;
using SupportTicketTriage.Domain.Events;
using SupportTicketTriage.Infrastructure.Serialization;

namespace SupportTicketTriage.Infrastructure.Events;

public static class EventEnvelopeSerializer
{
    public const string EventType = "TicketCreated";
    public const string SchemaVersion = "1.0";

    public static readonly JsonSerializerOptions Options = CreateOptions();

    public static TicketCreatedEventEnvelope ToEnvelope(TicketCreatedEvent domainEvent) =>
        new(
            domainEvent.EventId,
            EventType,
            SchemaVersion,
            domainEvent.CorrelationId,
            domainEvent.OccurredAtUtc,
            new TicketCreatedEventData(domainEvent.TicketId, domainEvent.Category, domainEvent.Priority));

    public static string Serialize(TicketCreatedEvent domainEvent) =>
        JsonSerializer.Serialize(ToEnvelope(domainEvent), Options);

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new UtcDateTimeJsonConverter());
        return options;
    }
}

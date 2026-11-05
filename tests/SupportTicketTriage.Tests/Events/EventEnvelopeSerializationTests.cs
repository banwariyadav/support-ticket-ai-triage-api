using System.Text.Json;
using SupportTicketTriage.Domain;
using SupportTicketTriage.Domain.Events;
using SupportTicketTriage.Infrastructure.Events;

namespace SupportTicketTriage.Tests.Events;

public sealed class EventEnvelopeSerializationTests
{
    [Fact]
    public void Serialize_UsesSchemaV1_AndOmitsPii()
    {
        var domainEvent = new TicketCreatedEvent
        {
            EventId = Guid.Parse("f25bfbe2-f91b-4c68-83d8-d828a1c64ef3"),
            TicketId = Guid.Parse("b6213028-f8f7-4d3b-8554-160343be52da"),
            Category = TicketCategories.Technical,
            Priority = TicketPriorities.Critical,
            CorrelationId = "8b14c62a-4a5a-4b83-a1ad-8ff8a33ef70d",
            OccurredAtUtc = new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = EventEnvelopeSerializer.Serialize(domainEvent);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("f25bfbe2-f91b-4c68-83d8-d828a1c64ef3", root.GetProperty("eventId").GetGuid().ToString());
        Assert.Equal("TicketCreated", root.GetProperty("eventType").GetString());
        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("8b14c62a-4a5a-4b83-a1ad-8ff8a33ef70d", root.GetProperty("correlationId").GetString());
        Assert.Equal("2026-11-04T12:00:00Z", root.GetProperty("occurredAtUtc").GetString());
        Assert.Equal("b6213028-f8f7-4d3b-8554-160343be52da", root.GetProperty("data").GetProperty("ticketId").GetGuid().ToString());
        Assert.Equal("Technical", root.GetProperty("data").GetProperty("category").GetString());
        Assert.Equal("Critical", root.GetProperty("data").GetProperty("priority").GetString());

        var raw = json.ToLowerInvariant();
        Assert.DoesNotContain("email", raw);
        Assert.DoesNotContain("customer", raw);
        Assert.DoesNotContain("description", raw);
        Assert.DoesNotContain("john", raw);
    }
}

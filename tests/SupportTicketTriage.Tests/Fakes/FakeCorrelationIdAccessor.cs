using SupportTicketTriage.Application.Abstractions;

namespace SupportTicketTriage.Tests.Fakes;

public sealed class FakeCorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = "11111111-1111-1111-1111-111111111111";
}

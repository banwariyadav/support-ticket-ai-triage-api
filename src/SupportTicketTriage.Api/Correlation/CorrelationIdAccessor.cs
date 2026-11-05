using SupportTicketTriage.Application.Abstractions;

namespace SupportTicketTriage.Api.Correlation;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = string.Empty;
}

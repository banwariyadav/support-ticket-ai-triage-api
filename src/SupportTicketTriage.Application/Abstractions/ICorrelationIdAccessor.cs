namespace SupportTicketTriage.Application.Abstractions;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}

namespace SupportTicketTriage.Application.Abstractions;

public interface IUtcClock
{
    DateTime UtcNow { get; }
}

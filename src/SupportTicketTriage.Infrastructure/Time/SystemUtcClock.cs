using SupportTicketTriage.Application.Abstractions;

namespace SupportTicketTriage.Infrastructure.Time;

public sealed class SystemUtcClock : IUtcClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

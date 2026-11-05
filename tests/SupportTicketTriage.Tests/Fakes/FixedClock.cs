using SupportTicketTriage.Application.Abstractions;

namespace SupportTicketTriage.Tests.Fakes;

public sealed class FixedClock : IUtcClock
{
    public FixedClock(DateTime utcNow) => UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);

    public DateTime UtcNow { get; set; }
}

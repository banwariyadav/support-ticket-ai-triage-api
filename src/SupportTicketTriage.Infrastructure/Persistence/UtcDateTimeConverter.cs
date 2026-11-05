using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SupportTicketTriage.Infrastructure.Persistence;

public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}

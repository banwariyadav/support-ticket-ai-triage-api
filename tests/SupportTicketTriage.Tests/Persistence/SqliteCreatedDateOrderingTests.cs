using Microsoft.EntityFrameworkCore;
using SupportTicketTriage.Domain;
using SupportTicketTriage.Domain.Entities;

namespace SupportTicketTriage.Tests.Persistence;

public sealed class SqliteCreatedDateOrderingTests
{
    [Fact]
    public async Task CreatedDate_IsOrderedInSqliteSql_NotClientSide()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var db = database.Context;

        var late = CreateTicket(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(2026, 11, 4, 18, 0, 0, DateTimeKind.Utc));
        var early = CreateTicket(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new DateTime(2026, 11, 4, 8, 0, 0, DateTimeKind.Utc));
        var middle = CreateTicket(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc));

        db.Tickets.AddRange(late, early, middle);
        await db.SaveChangesAsync();

        var query = db.Tickets
            .AsNoTracking()
            .OrderBy(t => t.CreatedDate)
            .ThenBy(t => t.Id);

        var sql = query.ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("CreatedDate", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DateTimeOffset", sql, StringComparison.OrdinalIgnoreCase);

        var ordered = await query.Select(t => t.Id).ToListAsync();
        Assert.Equal([early.Id, middle.Id, late.Id], ordered);

        var descSql = db.Tickets
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.Id)
            .ToQueryString();
        Assert.Contains("ORDER BY", descSql, StringComparison.OrdinalIgnoreCase);

        var readBack = await db.Tickets.AsNoTracking().SingleAsync(t => t.Id == early.Id);
        Assert.Equal(DateTimeKind.Utc, readBack.CreatedDate.Kind);
        Assert.Equal(early.CreatedDate, readBack.CreatedDate);
    }

    private static Ticket CreateTicket(Guid id, DateTime created) =>
        new()
        {
            Id = id,
            CustomerName = "Sort Test",
            Email = "sort@example.com",
            Description = "Ordering verification ticket",
            CreatedDate = created,
            Analysis = new TicketAnalysis
            {
                TicketId = id,
                Category = TicketCategories.General,
                Priority = TicketPriorities.Low,
                Summary = "Ordering verification ticket",
                AnalysedDate = created
            }
        };
}

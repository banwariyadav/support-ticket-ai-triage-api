using Microsoft.EntityFrameworkCore;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Application.Contracts;
using SupportTicketTriage.Application.Exceptions;
using SupportTicketTriage.Application.Tickets;
using SupportTicketTriage.Domain;
using SupportTicketTriage.Domain.Entities;
using SupportTicketTriage.Tests.Fakes;
using SupportTicketTriage.Tests.Persistence;

namespace SupportTicketTriage.Tests.Tickets;

public sealed class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_SavesBothRowsThenPublishesOnce()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var publisher = new FakeEventPublisher();
        var logger = new TestLogger<TicketService>();
        var sut = CreateSut(database.Context, publisher, logger);

        var response = await sut.CreateAsync(ValidRequest());

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(1, await database.Context.Tickets.CountAsync());
        Assert.Equal(1, await database.Context.TicketAnalyses.CountAsync());
        Assert.Single(publisher.Events);
        Assert.Equal(response.Id, publisher.Events[0].TicketId);
        Assert.Equal(response.Analysis!.Category, publisher.Events[0].Category);
        Assert.Contains("TicketId=", logger.Messages.Single(m => m.Contains("Ticket created", StringComparison.Ordinal)));
        Assert.All(logger.Messages, message =>
        {
            Assert.DoesNotContain("john@example.com", message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("John Smith", message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("production application", message, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task CreateAsync_DoesNotPublish_WhenAiFails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var publisher = new FakeEventPublisher();
        var ai = new FakeAiAnalysisService { Failure = new InvalidOperationException("model down") };
        var sut = CreateSut(database.Context, publisher, ai: ai);

        await Assert.ThrowsAsync<AiAnalysisException>(() => sut.CreateAsync(ValidRequest()));

        Assert.Empty(publisher.Events);
        Assert.Equal(0, await database.Context.Tickets.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_DoesNotPublish_WhenDatabaseSaveFails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var failing = new SaveFailingDbContext(database.Context);
        var publisher = new FakeEventPublisher();
        var sut = CreateSut(failing, publisher);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(ValidRequest()));
        Assert.Empty(publisher.Events);
    }

    [Fact]
    public async Task CreateAsync_KeepsTicket_WhenPublishFails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        var publisher = new FakeEventPublisher { Failure = new InvalidOperationException("broker timeout") };
        var sut = CreateSut(database.Context, publisher);

        var error = await Assert.ThrowsAsync<EventPublishException>(() => sut.CreateAsync(ValidRequest()));

        Assert.Equal(1, await database.Context.Tickets.CountAsync());
        Assert.NotEqual(Guid.Empty, error.TicketId);
        Assert.NotEqual(Guid.Empty, error.EventId);
    }

    [Fact]
    public async Task SearchAsync_FiltersAndPagesInProvider()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        SeedTickets(database.Context);
        var sut = CreateSut(database.Context, new FakeEventPublisher());

        var sql = database.Context.Tickets
            .AsNoTracking()
            .Where(t => t.Analysis != null && t.Analysis.Priority == TicketPriorities.High)
            .OrderByDescending(t => t.CreatedDate)
            .ThenByDescending(t => t.Id)
            .ToQueryString();
        Assert.Contains("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);

        var page = await sut.SearchAsync(new TicketSearchQuery
        {
            PageNumber = 1,
            PageSize = 1,
            Priority = "high",
            SortOrder = "desc"
        });

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
        Assert.Equal(TicketPriorities.High, page.Items[0].Analysis!.Priority);
    }

    private static TicketService CreateSut(
        IApplicationDbContext db,
        FakeEventPublisher publisher,
        TestLogger<TicketService>? logger = null,
        FakeAiAnalysisService? ai = null) =>
        new(
            db,
            ai ?? new FakeAiAnalysisService(),
            publisher,
            new FixedClock(new DateTime(2026, 11, 4, 12, 0, 0, DateTimeKind.Utc)),
            new FakeCorrelationIdAccessor(),
            logger ?? new TestLogger<TicketService>());

    private static CreateTicketRequest ValidRequest() =>
        new()
        {
            CustomerName = "John Smith",
            Email = "john@example.com",
            Description = "Our production application is down and users cannot log in. Urgent help needed."
        };

    private static void SeedTickets(Infrastructure.Persistence.ApplicationDbContext db)
    {
        db.Tickets.AddRange(
            Ticket("11111111-1111-1111-1111-111111111111", TicketPriorities.High, new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc)),
            Ticket("22222222-2222-2222-2222-222222222222", TicketPriorities.High, new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc)),
            Ticket("33333333-3333-3333-3333-333333333333", TicketPriorities.Low, new DateTime(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc)));
        db.SaveChanges();
    }

    private static Ticket Ticket(string id, string priority, DateTime created)
    {
        var guid = Guid.Parse(id);
        return new Ticket
        {
            Id = guid,
            CustomerName = "Seed",
            Email = "seed@example.com",
            Description = "Seeded ticket description",
            CreatedDate = created,
            Analysis = new TicketAnalysis
            {
                TicketId = guid,
                Category = TicketCategories.General,
                Priority = priority,
                Summary = "Seeded ticket description",
                AnalysedDate = created
            }
        };
    }
}

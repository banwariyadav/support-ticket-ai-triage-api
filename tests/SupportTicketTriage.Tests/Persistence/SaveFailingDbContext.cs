using Microsoft.EntityFrameworkCore;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Domain.Entities;

namespace SupportTicketTriage.Tests.Persistence;

public sealed class SaveFailingDbContext : IApplicationDbContext
{
    private readonly IApplicationDbContext _inner;

    public SaveFailingDbContext(IApplicationDbContext inner)
    {
        _inner = inner;
    }

    public DbSet<Ticket> Tickets => _inner.Tickets;
    public DbSet<TicketAnalysis> TicketAnalyses => _inner.TicketAnalyses;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("database unavailable");
}

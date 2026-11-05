using Microsoft.EntityFrameworkCore;
using SupportTicketTriage.Domain.Entities;

namespace SupportTicketTriage.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketAnalysis> TicketAnalyses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

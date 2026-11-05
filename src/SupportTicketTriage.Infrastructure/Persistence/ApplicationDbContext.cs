using Microsoft.EntityFrameworkCore;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Domain.Entities;

namespace SupportTicketTriage.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketAnalysis> TicketAnalyses => Set<TicketAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}

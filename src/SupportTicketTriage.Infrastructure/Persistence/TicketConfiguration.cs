using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketTriage.Domain.Entities;

namespace SupportTicketTriage.Infrastructure.Persistence;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Email)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(t => t.CreatedDate)
            .IsRequired()
            .HasConversion(new UtcDateTimeConverter());

        builder.HasIndex(t => t.CreatedDate)
            .HasDatabaseName("IX_Tickets_CreatedDate");

        builder.HasOne(t => t.Analysis)
            .WithOne(a => a.Ticket)
            .HasForeignKey<TicketAnalysis>(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

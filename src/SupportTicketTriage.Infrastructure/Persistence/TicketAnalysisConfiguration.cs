using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupportTicketTriage.Domain.Entities;

namespace SupportTicketTriage.Infrastructure.Persistence;

public sealed class TicketAnalysisConfiguration : IEntityTypeConfiguration<TicketAnalysis>
{
    public void Configure(EntityTypeBuilder<TicketAnalysis> builder)
    {
        builder.ToTable("TicketAnalyses");
        builder.HasKey(a => a.TicketId);

        builder.Property(a => a.Category)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(a => a.Priority)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(a => a.Summary)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.AnalysedDate)
            .IsRequired()
            .HasConversion(new UtcDateTimeConverter());

        builder.HasIndex(a => a.Priority)
            .HasDatabaseName("IX_TicketAnalyses_Priority");
    }
}

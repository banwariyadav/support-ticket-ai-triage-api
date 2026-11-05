namespace SupportTicketTriage.Domain.Entities;

public sealed class TicketAnalysis
{
    public Guid TicketId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTime AnalysedDate { get; set; }
    public Ticket Ticket { get; set; } = null!;
}

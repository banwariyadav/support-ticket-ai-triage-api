namespace SupportTicketTriage.Domain.Entities;

public sealed class Ticket
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public TicketAnalysis? Analysis { get; set; }
}

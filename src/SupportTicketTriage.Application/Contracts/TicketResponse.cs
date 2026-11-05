using SupportTicketTriage.Domain.Entities;

namespace SupportTicketTriage.Application.Contracts;

public sealed record TicketResponse(
    Guid Id,
    string CustomerName,
    string Email,
    string Description,
    DateTime CreatedDate,
    TicketAnalysisResponse? Analysis)
{
    public static TicketResponse From(Ticket ticket)
    {
        TicketAnalysisResponse? analysis = ticket.Analysis is null
            ? null
            : new TicketAnalysisResponse(
                ticket.Analysis.Category,
                ticket.Analysis.Priority,
                ticket.Analysis.Summary,
                ticket.Analysis.AnalysedDate);

        return new TicketResponse(
            ticket.Id,
            ticket.CustomerName,
            ticket.Email,
            ticket.Description,
            ticket.CreatedDate,
            analysis);
    }
}

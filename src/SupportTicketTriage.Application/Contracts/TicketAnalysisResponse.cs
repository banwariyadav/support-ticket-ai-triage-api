namespace SupportTicketTriage.Application.Contracts;

public sealed record TicketAnalysisResponse(
    string Category,
    string Priority,
    string Summary,
    DateTime AnalysedDate);

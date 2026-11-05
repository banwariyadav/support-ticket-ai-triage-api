namespace SupportTicketTriage.Application.Contracts;

public sealed record PagedTicketsResponse(
    IReadOnlyList<TicketResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);

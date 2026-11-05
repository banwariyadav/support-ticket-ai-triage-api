using SupportTicketTriage.Application.Contracts;

namespace SupportTicketTriage.Application.Tickets;

public interface ITicketService
{
    Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);
    Task<TicketResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedTicketsResponse> SearchAsync(TicketSearchQuery query, CancellationToken cancellationToken = default);
}

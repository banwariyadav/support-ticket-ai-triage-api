using Microsoft.AspNetCore.Mvc;
using SupportTicketTriage.Application.Contracts;
using SupportTicketTriage.Application.Tickets;

namespace SupportTicketTriage.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;

    public TicketsController(ITicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<TicketResponse>> Create(
        [FromBody] CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _tickets.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(id, cancellationToken);
        if (ticket is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Ticket not found",
                Detail = "No ticket exists for the supplied identifier.",
                Instance = HttpContext.Request.Path
            });
        }

        return Ok(ticket);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedTicketsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedTicketsResponse>> Search(
        [FromQuery] TicketSearchQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.SearchAsync(query, cancellationToken);
        return Ok(result);
    }
}

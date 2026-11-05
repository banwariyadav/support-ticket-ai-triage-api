using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Application.Ai;
using SupportTicketTriage.Application.Contracts;
using SupportTicketTriage.Application.Exceptions;
using SupportTicketTriage.Domain;
using SupportTicketTriage.Domain.Entities;
using SupportTicketTriage.Domain.Events;

namespace SupportTicketTriage.Application.Tickets;

public sealed class TicketService : ITicketService
{
    private readonly IApplicationDbContext _db;
    private readonly IAiAnalysisService _aiAnalysis;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUtcClock _clock;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<TicketService> _logger;

    public TicketService(
        IApplicationDbContext db,
        IAiAnalysisService aiAnalysis,
        IEventPublisher eventPublisher,
        IUtcClock clock,
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<TicketService> logger)
    {
        _db = db;
        _aiAnalysis = aiAnalysis;
        _eventPublisher = eventPublisher;
        _clock = clock;
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public async Task<TicketResponse> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = string.IsNullOrWhiteSpace(_correlationIdAccessor.CorrelationId)
            ? Guid.NewGuid().ToString()
            : _correlationIdAccessor.CorrelationId;

        AiAnalysisResult analysis;
        try
        {
            analysis = await _aiAnalysis.AnalyseAsync(request.Description.Trim(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI analysis failed. CorrelationId={CorrelationId} ExceptionCategory={ExceptionCategory}",
                correlationId, ex.GetType().Name);
            throw new AiAnalysisException("Ticket analysis failed.", ex);
        }

        ValidateAnalysis(analysis);

        var now = EnsureUtc(_clock.UtcNow);
        var ticketId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var ticket = new Ticket
        {
            Id = ticketId,
            CustomerName = request.CustomerName.Trim(),
            Email = request.Email.Trim(),
            Description = request.Description.Trim(),
            CreatedDate = now,
            Analysis = new TicketAnalysis
            {
                TicketId = ticketId,
                Category = analysis.Category,
                Priority = analysis.Priority,
                Summary = analysis.Summary,
                AnalysedDate = now
            }
        };

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(cancellationToken);

        var domainEvent = new TicketCreatedEvent
        {
            EventId = eventId,
            TicketId = ticketId,
            Category = analysis.Category,
            Priority = analysis.Priority,
            CorrelationId = correlationId,
            OccurredAtUtc = now
        };

        try
        {
            await _eventPublisher.PublishAsync(domainEvent, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Event publish failed after database commit. TicketId={TicketId} EventId={EventId} CorrelationId={CorrelationId} ExceptionCategory={ExceptionCategory} ElapsedMs={ElapsedMs}",
                ticketId,
                eventId,
                correlationId,
                ex.GetType().Name,
                stopwatch.ElapsedMilliseconds);
            throw new EventPublishException(ticketId, eventId, correlationId, ex);
        }

        _logger.LogInformation(
            "Ticket created. TicketId={TicketId} EventId={EventId} CorrelationId={CorrelationId} PublishOutcome={PublishOutcome} ElapsedMs={ElapsedMs}",
            ticketId,
            eventId,
            correlationId,
            "Published",
            stopwatch.ElapsedMilliseconds);

        return TicketResponse.From(ticket);
    }

    public async Task<TicketResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.Analysis)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return ticket is null ? null : TicketResponse.From(ticket);
    }

    public async Task<PagedTicketsResponse> SearchAsync(TicketSearchQuery query, CancellationToken cancellationToken = default)
    {
        var tickets = _db.Tickets
            .AsNoTracking()
            .Include(t => t.Analysis)
            .AsQueryable();

        if (query.NormalizedPriority is { } priority)
        {
            tickets = tickets.Where(t => t.Analysis != null && t.Analysis.Priority == priority);
        }

        tickets = query.NormalizedSortOrder == "asc"
            ? tickets.OrderBy(t => t.CreatedDate).ThenBy(t => t.Id)
            : tickets.OrderByDescending(t => t.CreatedDate).ThenByDescending(t => t.Id);

        var totalCount = await tickets.CountAsync(cancellationToken);
        var items = await tickets
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)query.PageSize);

        return new PagedTicketsResponse(
            items.Select(TicketResponse.From).ToList(),
            query.PageNumber,
            query.PageSize,
            totalCount,
            totalPages);
    }

    private static void ValidateAnalysis(AiAnalysisResult analysis)
    {
        if (!TicketCategories.IsKnown(analysis.Category)
            || !TicketPriorities.IsKnown(analysis.Priority)
            || string.IsNullOrWhiteSpace(analysis.Summary))
        {
            throw new AiAnalysisException("AI analysis returned an invalid category, priority, or empty summary.");
        }
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}

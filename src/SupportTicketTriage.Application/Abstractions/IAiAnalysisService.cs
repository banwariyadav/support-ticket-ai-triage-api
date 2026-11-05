using SupportTicketTriage.Application.Ai;

namespace SupportTicketTriage.Application.Abstractions;

public interface IAiAnalysisService
{
    Task<AiAnalysisResult> AnalyseAsync(string description, CancellationToken cancellationToken = default);
}

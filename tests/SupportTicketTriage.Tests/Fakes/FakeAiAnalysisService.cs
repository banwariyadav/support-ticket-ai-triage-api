using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Application.Ai;
using SupportTicketTriage.Domain;

namespace SupportTicketTriage.Tests.Fakes;

public sealed class FakeAiAnalysisService : IAiAnalysisService
{
    public AiAnalysisResult Result { get; set; } =
        new(TicketCategories.Technical, TicketPriorities.Critical, "summary");

    public Exception? Failure { get; set; }
    public int Calls { get; private set; }

    public Task<AiAnalysisResult> AnalyseAsync(string description, CancellationToken cancellationToken = default)
    {
        Calls++;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(Result);
    }
}

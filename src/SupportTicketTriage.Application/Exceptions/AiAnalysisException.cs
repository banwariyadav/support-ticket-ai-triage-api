namespace SupportTicketTriage.Application.Exceptions;

public sealed class AiAnalysisException : Exception
{
    public AiAnalysisException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

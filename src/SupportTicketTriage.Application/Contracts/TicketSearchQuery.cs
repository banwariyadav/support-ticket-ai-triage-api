using System.ComponentModel.DataAnnotations;
using SupportTicketTriage.Domain;

namespace SupportTicketTriage.Application.Contracts;

public sealed class TicketSearchQuery : IValidatableObject
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    public string? Priority { get; set; }

    public string SortOrder { get; set; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Priority) && !TicketPriorities.TryNormalize(Priority, out _))
        {
            yield return new ValidationResult(
                "Priority must be one of: Low, Medium, High, Critical.",
                [nameof(Priority)]);
        }

        if (!string.Equals(SortOrder, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(SortOrder, "desc", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "SortOrder must be 'asc' or 'desc'.",
                [nameof(SortOrder)]);
        }
    }

    public string NormalizedSortOrder =>
        string.Equals(SortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

    public string? NormalizedPriority =>
        TicketPriorities.TryNormalize(Priority, out var priority) ? priority : null;
}

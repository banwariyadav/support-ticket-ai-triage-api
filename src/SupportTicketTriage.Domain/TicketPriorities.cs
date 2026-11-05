namespace SupportTicketTriage.Domain;

public static class TicketPriorities
{
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Low, Medium, High, Critical
    };

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);

    public static bool TryNormalize(string? value, out string? normalized)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = null;
            return false;
        }

        foreach (var known in All)
        {
            if (string.Equals(known, value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                normalized = known;
                return true;
            }
        }

        normalized = null;
        return false;
    }
}

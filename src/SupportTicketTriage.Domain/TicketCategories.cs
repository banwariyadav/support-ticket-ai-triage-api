namespace SupportTicketTriage.Domain;

public static class TicketCategories
{
    public const string Billing = "Billing";
    public const string Technical = "Technical";
    public const string Account = "Account";
    public const string General = "General";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Billing, Technical, Account, General
    };

    public static bool IsKnown(string? value) =>
        !string.IsNullOrWhiteSpace(value) && All.Contains(value);
}

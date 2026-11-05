using System.Text.RegularExpressions;
using SupportTicketTriage.Application.Abstractions;
using SupportTicketTriage.Application.Ai;
using SupportTicketTriage.Domain;

namespace SupportTicketTriage.Infrastructure.Ai;

/// <summary>
/// Deterministic, offline classifier. Category and priority are evaluated independently.
/// Keyword precedence is first-match after normalization; unknown text falls back to General/Low.
/// The summary is a normalized/truncated extract of the source description, never a generated narrative.
/// </summary>
public sealed class MockAiAnalysisService : IAiAnalysisService
{
    public const int MaxSummaryLength = 280;

    // Precedence: Billing, then Technical, then Account, otherwise General.
    private static readonly string[] BillingKeywords =
        ["payment", "billing", "invoice", "charge", "charged", "refund", "subscription"];

    private static readonly string[] TechnicalKeywords =
        ["production", "application", "crash", "bug", "error", "outage", "down", "technical"];

    private static readonly string[] AccountKeywords =
        ["password", "account", "username", "login", "log in", "sign in", "reset"];

    // Precedence: Critical, then High, then Medium, otherwise Low.
    private static readonly string[] CriticalKeywords = ["critical", "outage"];
    private static readonly string[] HighKeywords = ["urgent", "widespread", "failure"];
    private static readonly string[] MediumKeywords = ["charged", "twice", "reset", "password", "cannot"];
    private static readonly string[] LowKeywords = ["question"];

    public Task<AiAnalysisResult> AnalyseAsync(string description, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = Normalize(description);
        var category = ClassifyCategory(normalized);
        var priority = ClassifyPriority(normalized);
        var summary = BuildSummary(normalized);

        return Task.FromResult(new AiAnalysisResult(category, priority, summary));
    }

    internal static string ClassifyCategory(string normalized)
    {
        if (ContainsAny(normalized, BillingKeywords))
        {
            return TicketCategories.Billing;
        }

        if (ContainsAny(normalized, TechnicalKeywords))
        {
            return TicketCategories.Technical;
        }

        if (ContainsAny(normalized, AccountKeywords))
        {
            return TicketCategories.Account;
        }

        return TicketCategories.General;
    }

    internal static string ClassifyPriority(string normalized)
    {
        if (IsCritical(normalized))
        {
            return TicketPriorities.Critical;
        }

        if (ContainsAny(normalized, HighKeywords))
        {
            return TicketPriorities.High;
        }

        if (ContainsAny(normalized, MediumKeywords))
        {
            return TicketPriorities.Medium;
        }

        if (ContainsAny(normalized, LowKeywords))
        {
            return TicketPriorities.Low;
        }

        return TicketPriorities.Low;
    }

    internal static string BuildSummary(string normalized)
    {
        if (normalized.Length <= MaxSummaryLength)
        {
            return normalized;
        }

        return normalized[..MaxSummaryLength].TrimEnd();
    }

    internal static string Normalize(string description)
    {
        var collapsed = Regex.Replace(description ?? string.Empty, @"\s+", " ").Trim();
        return collapsed;
    }

    private static bool IsCritical(string normalized)
    {
        if (ContainsAny(normalized, CriticalKeywords))
        {
            return true;
        }

        var hasProduction = ContainsToken(normalized, "production");
        var hasSevereImpact = ContainsToken(normalized, "down")
            || ContainsToken(normalized, "crash")
            || ContainsToken(normalized, "outage");

        return hasProduction && hasSevereImpact;
    }

    private static bool ContainsAny(string normalized, IEnumerable<string> keywords) =>
        keywords.Any(keyword => ContainsToken(normalized, keyword));

    private static bool ContainsToken(string normalized, string keyword)
    {
        if (keyword.Contains(' ', StringComparison.Ordinal))
        {
            return normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase);
        }

        return Regex.IsMatch(
            normalized,
            $@"\b{Regex.Escape(keyword)}\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

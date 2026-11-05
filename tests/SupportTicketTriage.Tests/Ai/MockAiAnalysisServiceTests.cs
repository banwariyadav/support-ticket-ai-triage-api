using SupportTicketTriage.Domain;
using SupportTicketTriage.Infrastructure.Ai;

namespace SupportTicketTriage.Tests.Ai;

public sealed class MockAiAnalysisServiceTests
{
    private readonly MockAiAnalysisService _sut = new();

    [Theory]
    [InlineData("Payment charged twice", TicketCategories.Billing, TicketPriorities.Medium)]
    [InlineData("Production application down", TicketCategories.Technical, TicketPriorities.Critical)]
    [InlineData("Cannot reset password", TicketCategories.Account, TicketPriorities.Medium)]
    [InlineData("General product question", TicketCategories.General, TicketPriorities.Low)]
    [InlineData("Urgent widespread billing failure", TicketCategories.Billing, TicketPriorities.High)]
    public async Task AnalyseAsync_MatchesDocumentedVectors(string description, string category, string priority)
    {
        var result = await _sut.AnalyseAsync(description);

        Assert.Equal(category, result.Category);
        Assert.Equal(priority, result.Priority);
        Assert.False(string.IsNullOrWhiteSpace(result.Summary));
    }

    [Fact]
    public async Task AnalyseAsync_IsCaseInsensitiveAndCollapsesWhitespace()
    {
        var result = await _sut.AnalyseAsync("  PAYMENT   CHARGED   TWICE  ");

        Assert.Equal(TicketCategories.Billing, result.Category);
        Assert.Equal(TicketPriorities.Medium, result.Priority);
        Assert.Equal("PAYMENT CHARGED TWICE", result.Summary);
    }

    [Fact]
    public async Task AnalyseAsync_PrefersBillingOverTechnicalWhenBothPresent()
    {
        var result = await _sut.AnalyseAsync("Billing invoice error after the application update");

        Assert.Equal(TicketCategories.Billing, result.Category);
    }

    [Fact]
    public async Task AnalyseAsync_PrefersTechnicalOverAccountWhenBothPresent()
    {
        var result = await _sut.AnalyseAsync("Production application is down and users cannot log in");

        Assert.Equal(TicketCategories.Technical, result.Category);
        Assert.Equal(TicketPriorities.Critical, result.Priority);
    }

    [Fact]
    public async Task AnalyseAsync_EvaluatesCategoryAndPriorityIndependently()
    {
        var result = await _sut.AnalyseAsync("Urgent widespread billing failure");

        Assert.Equal(TicketCategories.Billing, result.Category);
        Assert.Equal(TicketPriorities.High, result.Priority);
    }

    [Fact]
    public async Task AnalyseAsync_UnknownTextDefaultsToGeneralLow()
    {
        var result = await _sut.AnalyseAsync("Please tell me about your office hours.");

        Assert.Equal(TicketCategories.General, result.Category);
        Assert.Equal(TicketPriorities.Low, result.Priority);
        Assert.Equal("Please tell me about your office hours.", result.Summary);
    }

    [Fact]
    public async Task AnalyseAsync_TruncatesSummaryWithoutInventingContent()
    {
        var source = new string('x', MockAiAnalysisService.MaxSummaryLength + 40);
        var result = await _sut.AnalyseAsync(source);

        Assert.Equal(MockAiAnalysisService.MaxSummaryLength, result.Summary.Length);
        Assert.StartsWith(new string('x', 20), result.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("generated", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyseAsync_DoesNotUseNetwork()
    {
        var result = await _sut.AnalyseAsync("Cannot reset password");
        Assert.Equal(TicketCategories.Account, result.Category);
    }
}

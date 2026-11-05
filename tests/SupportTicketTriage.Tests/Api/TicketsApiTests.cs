using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SupportTicketTriage.Application.Contracts;
using SupportTicketTriage.Infrastructure.Persistence;

namespace SupportTicketTriage.Tests.Api;

public sealed class TicketsApiTests
{
    [Fact]
    public async Task Post_CreatesTicketAnalysisAndEvent_WithLocation()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tickets")
        {
            Content = JsonContent.Create(new
            {
                customerName = "Jane Tester",
                email = "jane.tester@example.com",
                description = "Production application down and users cannot log in."
            })
        };
        request.Headers.Add("X-Correlation-ID", "dddddddd-dddd-dddd-dddd-dddddddddddd");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(payload);
        Assert.Equal("/api/tickets/" + payload!.Id, response.Headers.Location?.AbsolutePath);
        Assert.Equal("Technical", payload.Analysis!.Category);
        Assert.Equal("Critical", payload.Analysis.Priority);
        Assert.Equal("dddddddd-dddd-dddd-dddd-dddddddddddd", response.Headers.GetValues("X-Correlation-ID").Single());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, db.Tickets.Count());
        Assert.Equal(1, db.TicketAnalyses.Count());
        Assert.Single(factory.Publisher.Events);
        Assert.Equal(payload.Id, factory.Publisher.Events[0].TicketId);
        Assert.Equal("dddddddd-dddd-dddd-dddd-dddddddddddd", factory.Publisher.Events[0].CorrelationId);
    }

    [Fact]
    public async Task GetById_Returns200Then404()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var created = await CreateTicketAsync(client, "Cannot reset password on the portal today.");
        var found = await client.GetAsync($"/api/tickets/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);

        var missing = await client.GetAsync($"/api/tickets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var problem = await missing.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.TryGetProperty("title", out _) || problem.TryGetProperty("status", out _));
    }

    [Fact]
    public async Task GetById_InvalidGuid_Returns400()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/tickets/not-a-guid");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetList_FiltersPriorityAndOrdersByCreatedDate()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        await CreateTicketAsync(client, "General product question about office hours.");
        await CreateTicketAsync(client, "Urgent widespread billing failure affecting invoices.");
        await CreateTicketAsync(client, "Payment charged twice on last invoice statement.");

        var filtered = await client.GetFromJsonAsync<PagedTicketsResponse>("/api/tickets?priority=High&sortOrder=desc");
        Assert.NotNull(filtered);
        Assert.All(filtered!.Items, item => Assert.Equal("High", item.Analysis!.Priority));

        var page = await client.GetFromJsonAsync<PagedTicketsResponse>("/api/tickets?pageNumber=1&pageSize=2&sortOrder=desc");
        Assert.NotNull(page);
        Assert.Equal(2, page!.Items.Count);
        Assert.True(page.TotalCount >= 3);
        Assert.True(page.Items[0].CreatedDate >= page.Items[1].CreatedDate);

        var empty = await client.GetFromJsonAsync<PagedTicketsResponse>("/api/tickets?priority=Critical&pageNumber=5");
        Assert.NotNull(empty);
        Assert.Empty(empty!.Items);
    }

    [Fact]
    public async Task Post_InvalidBody_Returns400_WithoutPublishing()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerName = " ",
            email = "not-an-email",
            description = "short"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(factory.Publisher.Events);
    }

    [Fact]
    public async Task GetList_InvalidQuery_Returns400()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/tickets?pageNumber=0&priority=Urgent&sortOrder=sideways");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_BrokerFailure_Returns503_AndKeepsTicket()
    {
        await using var factory = new ApiFactory();
        factory.Publisher.Failure = new InvalidOperationException("send failed");
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerName = "Retry User",
            email = "retry.user@example.com",
            description = "General product question about billing hours."
        });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, db.Tickets.Count());
    }

    private static async Task<TicketResponse> CreateTicketAsync(HttpClient client, string description)
    {
        var response = await client.PostAsJsonAsync("/api/tickets", new
        {
            customerName = "API Tester",
            email = "api.tester@example.com",
            description
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TicketResponse>())!;
    }
}

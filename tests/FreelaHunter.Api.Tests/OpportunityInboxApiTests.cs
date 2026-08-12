using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Contracts.Inbox;
using FreelaHunter.Contracts.Opportunities;

namespace FreelaHunter.Api.Tests;

[Collection("Integration")]
public class OpportunityInboxApiTests
{
    [Fact]
    public async Task GetList_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opportunities");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetList_EmptyDb_ReturnsEmptyPage()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities");

        Assert.NotNull(response);
        Assert.Empty(response!.Items);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(0, response.TotalPages);
    }

    [Fact]
    public async Task GetList_WithOpportunities_ReturnsPaginated()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest($"Project {i}"));
        }

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities?page=1&pageSize=2");

        Assert.NotNull(response);
        Assert.Equal(2, response!.Items.Count);
        Assert.Equal(1, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
    }

    [Fact]
    public async Task GetList_SecondPage()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest($"Project {i}"));
        }

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities?page=2&pageSize=2");

        Assert.NotNull(response);
        Assert.Equal(2, response!.Items.Count);
        Assert.Equal(2, response.Page);
    }

    [Fact]
    public async Task GetList_Search()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest("WhatsApp Bot"));
        await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest("React Dashboard"));

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities?search=whatsapp");

        Assert.NotNull(response);
        Assert.Single(response!.Items);
        Assert.Equal("WhatsApp Bot", response.Items[0].Title);
    }

    [Fact]
    public async Task GetList_FilterMarketplace()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var req1 = CreateRequest("Workana Project");
        req1.Marketplace = ContractMarketplace.Workana;
        var req2 = CreateRequest("Upwork Project");
        req2.Marketplace = ContractMarketplace.Upwork;

        await client.PostAsJsonAsync("/api/opportunities/manual", req1);
        await client.PostAsJsonAsync("/api/opportunities/manual", req2);

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities?marketplace=Workana");

        Assert.NotNull(response);
        Assert.Single(response!.Items);
        Assert.Equal(ContractMarketplace.Workana, response.Items[0].Marketplace);
    }

    [Fact]
    public async Task GetList_FilterStatus()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest("Test"));
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/shortlist", new object());

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities?status=Shortlisted");

        Assert.NotNull(response);
        Assert.Single(response!.Items);
        Assert.Equal(ContractOpportunityStatus.Shortlisted, response.Items[0].Status);
    }

    [Fact]
    public async Task GetList_SortTitleAsc()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest("Zebra Project"));
        await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest("Alpha Project"));

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities?sort=TitleAsc");

        Assert.NotNull(response);
        Assert.Equal("Alpha Project", response!.Items[0].Title);
        Assert.Equal("Zebra Project", response.Items[1].Title);
    }

    [Fact]
    public async Task GetList_ZeroResults()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetFromJsonAsync<PagedOpportunityResponse>("/api/opportunities?search=nonexistent");

        Assert.NotNull(response);
        Assert.Empty(response!.Items);
        Assert.Equal(0, response.TotalCount);
    }

    [Fact]
    public async Task GetList_InvalidPage_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opportunities?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetList_InvalidPageSize_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opportunities?pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetList_InvalidDateRange_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/opportunities?capturedFrom=2025-06-01&capturedTo=2025-01-01");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static CreateManualOpportunityRequest CreateRequest(string title)
    {
        return new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = title,
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Amount = 1000m,
                Currency = "BRL"
            },
            Client = new ClientInput
            {
                DisplayName = "Client",
                Country = "BR"
            },
            Competition = new CompetitionInput
            {
                ProposalCount = 5
            },
            RequiredSkills = ["C#"]
        };
    }
}

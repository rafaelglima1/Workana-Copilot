using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Contracts.Opportunities;

namespace FreelaHunter.Api.Tests;

[Collection("Integration")]
public class OpportunityStatusApiTests
{
    [Fact]
    public async Task Shortlist_Existing_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/shortlist", new object());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.Equal(ContractOpportunityStatus.Shortlisted, result!.Status);
    }

    [Fact]
    public async Task Shortlist_PersistsViaGet()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/shortlist", new object());

        var getResponse = await client.GetFromJsonAsync<OpportunityResponse>($"/api/opportunities/{created!.Id}");
        Assert.Equal(ContractOpportunityStatus.Shortlisted, getResponse!.Status);
    }

    [Fact]
    public async Task Shortlist_NotFound_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opportunities/{Guid.NewGuid()}/shortlist", new object());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Shortlist_Idempotent()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/shortlist", new object());
        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/shortlist", new object());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ignore_Existing_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/ignore", new object());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.Equal(ContractOpportunityStatus.Ignored, result!.Status);
    }

    [Fact]
    public async Task Ignore_PersistsViaGet()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/ignore", new object());

        var getResponse = await client.GetFromJsonAsync<OpportunityResponse>($"/api/opportunities/{created!.Id}");
        Assert.Equal(ContractOpportunityStatus.Ignored, getResponse!.Status);
    }

    [Fact]
    public async Task Ignore_NotFound_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opportunities/{Guid.NewGuid()}/ignore", new object());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Ignore_Idempotent()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/ignore", new object());
        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/ignore", new object());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Archive_Existing_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/archive", new object());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OpportunityResponse>();
        Assert.Equal(ContractOpportunityStatus.Archived, result!.Status);
    }

    [Fact]
    public async Task Archive_PersistsViaGet()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/archive", new object());

        var getResponse = await client.GetFromJsonAsync<OpportunityResponse>($"/api/opportunities/{created!.Id}");
        Assert.Equal(ContractOpportunityStatus.Archived, getResponse!.Status);
    }

    [Fact]
    public async Task Archive_NotFound_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/opportunities/{Guid.NewGuid()}/archive", new object());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Archive_Idempotent()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/archive", new object());
        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/archive", new object());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Shortlist_AfterArchive_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/archive", new object());
        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/shortlist", new object());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ignore_AfterArchive_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/archive", new object());
        var response = await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/ignore", new object());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullWorkflow_Shortlist_Ignore_Archive()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", CreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        // Shortlist
        await client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/shortlist", new object());
        var get1 = await client.GetFromJsonAsync<OpportunityResponse>($"/api/opportunities/{created.Id}");
        Assert.Equal(ContractOpportunityStatus.Shortlisted, get1!.Status);

        // Ignore
        await client.PostAsJsonAsync($"/api/opportunities/{created.Id}/ignore", new object());
        var get2 = await client.GetFromJsonAsync<OpportunityResponse>($"/api/opportunities/{created.Id}");
        Assert.Equal(ContractOpportunityStatus.Ignored, get2!.Status);

        // Archive
        await client.PostAsJsonAsync($"/api/opportunities/{created.Id}/archive", new object());
        var get3 = await client.GetFromJsonAsync<OpportunityResponse>($"/api/opportunities/{created.Id}");
        Assert.Equal(ContractOpportunityStatus.Archived, get3!.Status);
    }

    private static CreateManualOpportunityRequest CreateRequest()
    {
        return new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test Project",
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

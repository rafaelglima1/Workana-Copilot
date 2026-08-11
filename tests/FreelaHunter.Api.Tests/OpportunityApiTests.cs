using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Contracts.Opportunities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FreelaHunter.Api.Tests;

public class OpportunityApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OpportunityApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostManualOpportunity_Returns201()
    {
        var request = CreateValidRequest();

        var response = await _client.PostAsJsonAsync("/api/opportunities/manual", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostManualOpportunity_ReturnsNormalizedResponse()
    {
        var request = CreateValidRequest();
        request.Title = "  Test Project  ";
        request.RequiredSkills = ["  C#  ", "c#"];

        var response = await _client.PostAsJsonAsync("/api/opportunities/manual", request);
        var result = await response.Content.ReadFromJsonAsync<OpportunityResponse>();

        Assert.NotNull(result);
        Assert.Equal("Test Project", result!.Title);
        Assert.Single(result.RequiredSkills);
        Assert.Contains("C#", result.RequiredSkills);
    }

    [Fact]
    public async Task PostManualOpportunity_InvalidInput_Returns400()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "",
            Description = "Description"
        };

        var response = await _client.PostAsJsonAsync("/api/opportunities/manual", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetOpportunity_Existing_Returns200()
    {
        var postRequest = CreateValidRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", postRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var getResponse = await _client.GetAsync($"/api/opportunities/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetOpportunity_NonExisting_Returns404()
    {
        var response = await _client.GetAsync($"/api/opportunities/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOpportunity_InvalidId_Returns400()
    {
        var response = await _client.GetAsync("/api/opportunities/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostFollowedByGet_ReturnsSameOpportunity()
    {
        var postRequest = CreateValidRequest();
        postRequest.Title = "Integration Test";
        postRequest.Marketplace = ContractMarketplace.Upwork;

        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", postRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var getResponse = await _client.GetAsync($"/api/opportunities/{created!.Id}");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved!.Id);
        Assert.Equal("Integration Test", retrieved.Title);
        Assert.Equal(ContractMarketplace.Upwork, retrieved.Marketplace);
        Assert.Equal(ContractIntakeChannel.Manual, retrieved.IntakeChannel);
        Assert.Equal(ContractOpportunityStatus.New, retrieved.Status);
    }

    [Fact]
    public async Task GetHealth_StillReturns200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static CreateManualOpportunityRequest CreateValidRequest()
    {
        return new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test Project",
            Description = "A test project description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Amount = 1000m,
                Currency = "BRL"
            },
            Client = new ClientInput
            {
                DisplayName = "Test Client",
                Country = "BR"
            },
            Competition = new CompetitionInput
            {
                ProposalCount = 5
            },
            RequiredSkills = ["C#", "Blazor"]
        };
    }
}

using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Contracts.Opportunities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FreelaHunter.Api.Tests;

public class OpportunityScoringApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OpportunityScoringApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ScoreOpportunity_Existing_Returns200()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = CreateScoreRequest();
        var response = await _client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/score", scoreRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScoreOpportunity_ReturnsValidResponse()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = CreateScoreRequest();
        var response = await _client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/score", scoreRequest);
        var result = await response.Content.ReadFromJsonAsync<OpportunityScoreResponse>();

        Assert.NotNull(result);
        Assert.InRange(result!.Overall, 0, 100);
        Assert.False(string.IsNullOrEmpty(result.Recommendation));
        Assert.NotNull(result.TechnicalFit);
        Assert.NotNull(result.EconomicPotential);
        Assert.NotNull(result.ClientQuality);
        Assert.NotNull(result.Competition);
        Assert.NotNull(result.DeliveryConfidence);
        Assert.NotNull(result.AiLeverage);
    }

    [Fact]
    public async Task ScoreOpportunity_NonExisting_Returns404()
    {
        var scoreRequest = CreateScoreRequest();
        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{Guid.NewGuid()}/score", scoreRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScoreOpportunity_InvalidId_Returns400()
    {
        var scoreRequest = CreateScoreRequest();
        var response = await _client.PostAsJsonAsync(
            "/api/opportunities/00000000-0000-0000-0000-000000000000/score", scoreRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScoreOpportunity_InvalidRequest_Returns400()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var invalidRequest = new { };
        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/score", invalidRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScoreOpportunity_Deterministic()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = CreateScoreRequest();
        var response1 = await _client.PostAsJsonAsync($"/api/opportunities/{created!.Id}/score", scoreRequest);
        var result1 = await response1.Content.ReadFromJsonAsync<OpportunityScoreResponse>();

        var response2 = await _client.PostAsJsonAsync($"/api/opportunities/{created.Id}/score", scoreRequest);
        var result2 = await response2.Content.ReadFromJsonAsync<OpportunityScoreResponse>();

        Assert.Equal(result1!.Overall, result2!.Overall);
        Assert.Equal(result1.Recommendation, result2.Recommendation);
    }

    [Fact]
    public async Task GetHealth_StillReturns200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ExistingTests_StillPass()
    {
        var postRequest = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test Project",
            Description = "A test project description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Basis = ContractBudgetBasis.FixedProject,
                Amount = 1000m,
                Currency = "BRL"
            }
        };

        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", postRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var healthResponse = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }

    private static CreateManualOpportunityRequest CreateManualRequest()
    {
        return new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Scoring Test Project",
            Description = "A project for scoring tests",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Range,
                Basis = ContractBudgetBasis.FixedProject,
                Minimum = 5000,
                Maximum = 15000,
                Currency = "BRL"
            },
            Client = new ClientInput
            {
                PublishedProjects = 3,
                PaidProjects = 0
            },
            Competition = new CompetitionInput
            {
                ProposalCount = 54
            },
            RequiredSkills = ["C#", "API", "Flutter"]
        };
    }

    private static ScoreOpportunityRequest CreateScoreRequest()
    {
        return new ScoreOpportunityRequest
        {
            Skills =
            [
                new SkillInput { Name = "C#", Level = ContractSkillLevel.Expert },
                new SkillInput { Name = "API", Level = ContractSkillLevel.Expert }
            ],
            TargetHourlyRate = new RateTargetInput { Amount = 100, Currency = "BRL" },
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };
    }
}

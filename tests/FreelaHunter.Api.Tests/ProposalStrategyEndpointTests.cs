using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Application;
using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Api.Tests;

[Collection("Integration")]
public class ProposalStrategyEndpointTests
{
    [Fact]
    public async Task ProposalStrategy_WithEvaluation_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);
        var opportunityId = await CreateOpportunity(client);
        await EvaluateOpportunity(client, opportunityId);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/proposal-strategy", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProposalStrategy_ReturnsDecision()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);
        var opportunityId = await CreateOpportunity(client);
        await EvaluateOpportunity(client, opportunityId);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/proposal-strategy", new { });
        var result = await response.Content.ReadFromJsonAsync<ProposalStrategyResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Decision);
        Assert.True(result.Decision == "Go" || result.Decision == "Maybe" || result.Decision == "NoGo");
    }

    [Fact]
    public async Task ProposalStrategy_HasDeliveryDays()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);
        var opportunityId = await CreateOpportunity(client);
        await EvaluateOpportunity(client, opportunityId);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/proposal-strategy", new { });
        var result = await response.Content.ReadFromJsonAsync<ProposalStrategyResponse>();

        Assert.NotNull(result);
        Assert.True(result!.EstimatedDeliveryDays > 0);
    }

    [Fact]
    public async Task ProposalStrategy_HasRevenue()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);
        var opportunityId = await CreateOpportunity(client);
        await EvaluateOpportunity(client, opportunityId);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/proposal-strategy", new { });
        var result = await response.Content.ReadFromJsonAsync<ProposalStrategyResponse>();

        Assert.NotNull(result);
        Assert.True(result!.EstimatedRevenue > 0);
    }

    [Fact]
    public async Task ProposalStrategy_HasRationale()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);
        var opportunityId = await CreateOpportunity(client);
        await EvaluateOpportunity(client, opportunityId);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/proposal-strategy", new { });
        var result = await response.Content.ReadFromJsonAsync<ProposalStrategyResponse>();

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Rationale);
    }

    [Fact]
    public async Task ProposalStrategy_NoEvaluation_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);
        var opportunityId = await CreateOpportunity(client);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/proposal-strategy", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProposalStrategy_NoProfile_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var opportunityId = await CreateOpportunity(client);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/proposal-strategy", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProposalStrategy_NonExistingOpportunity_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{Guid.NewGuid()}/proposal-strategy", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProposalStrategy_InvalidId_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/opportunities/00000000-0000-0000-0000-000000000000/proposal-strategy", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task SetupProfile(HttpClient client)
    {
        var profileRequest = new SaveFreelancerProfileRequest
        {
            Skills = [
                new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert },
                new ProfileSkillInput { Name = "API", Level = ContractSkillLevel.Expert }
            ],
            HourlyRateTargets = [
                new ProfileRateInput { Amount = 100, Currency = "BRL" }
            ],
            DefaultRateCurrency = "BRL",
            WeeklyCapacityHours = 20m
        };

        await client.PutAsJsonAsync("/api/freelancer-profile", profileRequest);
    }

    private static async Task<Guid> CreateOpportunity(HttpClient client)
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test Proposal Strategy",
            Description = "Build a REST API with ASP.NET Core",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Basis = ContractBudgetBasis.FixedProject,
                Amount = 8000,
                Currency = "BRL"
            },
            RequiredSkills = ["C#", "API"]
        };

        var response = await client.PostAsJsonAsync("/api/opportunities/manual", request);
        var created = await response.Content.ReadFromJsonAsync<OpportunityResponse>();
        return created!.Id;
    }

    private static async Task EvaluateOpportunity(HttpClient client, Guid opportunityId)
    {
        await client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });
    }
}

using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Application;
using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Api.Tests;

public class OpportunityEvaluationApiTests : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var factory = new WebApplicationFactory<Program>();
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IFreelancerProfileRepository));
                if (descriptor is not null)
                    services.Remove(descriptor);
                services.AddSingleton<IFreelancerProfileRepository, InMemoryFreelancerProfileRepository>();

                var gatewayDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IOpportunityIntelligenceGateway));
                if (gatewayDescriptor is not null)
                    services.Remove(gatewayDescriptor);
                services.AddSingleton<IOpportunityIntelligenceGateway, FakeApiIntelligenceGateway>();
            });
        }).CreateClient();
        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Evaluate_HappyPath_Returns200()
    {
        await SetupProfile();
        var opportunityId = await CreateOpportunity();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_HasAnalysis()
    {
        await SetupProfile();
        var opportunityId = await CreateOpportunity();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });
        var result = await response.Content.ReadFromJsonAsync<OpportunityEvaluationResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Analysis);
        Assert.Equal(120m, result.Analysis.EstimatedHours);
    }

    [Fact]
    public async Task Evaluate_HasScore()
    {
        await SetupProfile();
        var opportunityId = await CreateOpportunity();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });
        var result = await response.Content.ReadFromJsonAsync<OpportunityEvaluationResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Score);
        Assert.InRange(result.Score.Overall, 0, 100);
        Assert.False(string.IsNullOrEmpty(result.Score.Recommendation));
    }

    [Fact]
    public async Task Evaluate_HasScoringInputs()
    {
        await SetupProfile();
        var opportunityId = await CreateOpportunity();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });
        var result = await response.Content.ReadFromJsonAsync<OpportunityEvaluationResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result!.ScoringInputs);
        Assert.NotEmpty(result.ScoringInputs.ProfileSkills);
        Assert.NotNull(result.ScoringInputs.TargetHourlyRate);
    }

    [Fact]
    public async Task Evaluate_HasSources()
    {
        await SetupProfile();
        var opportunityId = await CreateOpportunity();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });
        var result = await response.Content.ReadFromJsonAsync<OpportunityEvaluationResponse>();

        Assert.NotNull(result);
        Assert.Equal("Opportunity.RequiredSkills + FreelancerProfile.Skills",
            result!.ScoringInputs.TechnicalFitSource);
        Assert.Equal("OpportunitySemanticAnalysis",
            result.ScoringInputs.EstimatedHoursSource);
        Assert.Equal("FreelancerProfile",
            result.ScoringInputs.TargetHourlyRateSource);
    }

    [Fact]
    public async Task Evaluate_InvalidId_Returns400()
    {
        await SetupProfile();

        var response = await _client.PostAsJsonAsync(
            "/api/opportunities/00000000-0000-0000-0000-000000000000/evaluate", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_NonExistingOpportunity_Returns404()
    {
        await SetupProfile();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{Guid.NewGuid()}/evaluate", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_NoProfile_Returns409()
    {
        var opportunityId = await CreateOpportunity();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_Health_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Evaluate_OpportunityNotModified()
    {
        await SetupProfile();
        var opportunityId = await CreateOpportunity();

        await _client.PostAsJsonAsync(
            $"/api/opportunities/{opportunityId}/evaluate", new { });

        var getResponse = await _client.GetAsync($"/api/opportunities/{opportunityId}");
        var opportunity = await getResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        Assert.NotNull(opportunity);
        Assert.Equal("Test Evaluate API", opportunity!.Title);
    }

    private async Task SetupProfile()
    {
        var profileRequest = new SaveFreelancerProfileRequest
        {
            Skills = [
                new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert },
                new ProfileSkillInput { Name = "API", Level = ContractSkillLevel.Expert },
                new ProfileSkillInput { Name = "Flutter", Level = ContractSkillLevel.None }
            ],
            HourlyRateTargets = [
                new ProfileRateInput { Amount = 100, Currency = "BRL" },
                new ProfileRateInput { Amount = 65, Currency = "USD" }
            ],
            DefaultRateCurrency = "BRL"
        };

        await _client.PutAsJsonAsync("/api/freelancer-profile", profileRequest);
    }

    private async Task<Guid> CreateOpportunity()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test Evaluate API",
            Description = "Build a REST API with ASP.NET Core",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Range,
                Basis = ContractBudgetBasis.FixedProject,
                Minimum = 5000,
                Maximum = 15000,
                Currency = "BRL"
            },
            RequiredSkills = ["C#", "API", "Flutter"]
        };

        var response = await _client.PostAsJsonAsync("/api/opportunities/manual", request);
        var created = await response.Content.ReadFromJsonAsync<OpportunityResponse>();
        return created!.Id;
    }
}

internal sealed class FakeApiIntelligenceGateway : IOpportunityIntelligenceGateway
{
    public Task<OpportunitySemanticAnalysis> AnalyzeAsync(
        OpportunityIntelligenceInput input,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new OpportunitySemanticAnalysis
        {
            EstimatedHours = 120,
            DeliveryRisk = DeliveryRiskLevel.Moderate,
            AiLeverage = AiLeverageLevel.High,
            Confidence = AnalysisConfidence.Medium,
            TechnicalRequirements = ["C#", "ASP.NET Core"],
            RiskFactors = ["Tight deadline"],
            AiLeverageReasons = ["CRUD operations"],
            ClarifyingQuestions = [],
            Usage = new LlmUsage
            {
                InputTokens = 100,
                OutputTokens = 200,
                TotalTokens = 300,
                Provider = "MiMo",
                Model = "mimo-v2.5"
            }
        });
    }
}

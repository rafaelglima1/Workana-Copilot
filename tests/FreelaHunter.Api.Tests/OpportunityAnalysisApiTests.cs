using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Application;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Api.Tests;

public class OpportunityAnalysisApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly FakeAnalysisGateway _fakeGateway = new();

    public OpportunityAnalysisApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IOpportunityIntelligenceGateway));
                if (descriptor is not null)
                    services.Remove(descriptor);

                services.AddSingleton<IOpportunityIntelligenceGateway>(_fakeGateway);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task AnalyzeOpportunity_Existing_Returns200()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_ReturnsValidStructure()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });
        var result = await response.Content.ReadFromJsonAsync<OpportunityAnalysisResponse>();

        Assert.NotNull(result);
        Assert.True(result!.EstimatedHours > 0);
        Assert.False(string.IsNullOrEmpty(result.DeliveryRisk));
        Assert.False(string.IsNullOrEmpty(result.AiLeverage));
        Assert.False(string.IsNullOrEmpty(result.Confidence));
        Assert.NotNull(result.TechnicalRequirements);
        Assert.NotNull(result.RiskFactors);
        Assert.NotNull(result.AiLeverageReasons);
        Assert.NotNull(result.ClarifyingQuestions);
    }

    [Fact]
    public async Task AnalyzeOpportunity_NonExisting_Returns404()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{Guid.NewGuid()}/analyze", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_InvalidId_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/opportunities/00000000-0000-0000-0000-000000000000/analyze", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_ProviderFailure_Returns503()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        _fakeGateway.ShouldFail = true;
        _fakeGateway.FailType = FailType.Unavailable;

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_InvalidProviderResponse_Returns502()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        _fakeGateway.ShouldFail = true;
        _fakeGateway.FailType = FailType.InvalidResponse;

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_StillReturns200()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_InputFieldsCorrect()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        _fakeGateway.ShouldFail = false;
        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(_fakeGateway.LastInput);
        Assert.Equal("Test CRUD Project", _fakeGateway.LastInput!.Title);
    }

    [Fact]
    public async Task AnalyzeOpportunity_ReturnsUsageMetadata()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });
        var result = await response.Content.ReadFromJsonAsync<OpportunityAnalysisResponse>();

        Assert.NotNull(result);
        Assert.NotNull(result!.Usage);
        Assert.Equal("MiMo", result.Usage!.Provider);
        Assert.Equal("mimo-v2.5", result.Usage.Model);
        Assert.Equal(100, result.Usage.InputTokens);
        Assert.Equal(200, result.Usage.OutputTokens);
        Assert.Equal(300, result.Usage.TotalTokens);
    }

    private static CreateManualOpportunityRequest CreateManualRequest()
    {
        return new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test CRUD Project",
            Description = "Build a CRUD API with ASP.NET Core",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Basis = ContractBudgetBasis.FixedProject,
                Amount = 10000,
                Currency = "BRL"
            },
            Category = "Web Development",
            Subcategory = "API",
            RequiredSkills = ["C#", "ASP.NET Core", "PostgreSQL"]
        };
    }
}

internal enum FailType
{
    Unavailable,
    InvalidResponse
}

internal sealed class FakeAnalysisGateway : IOpportunityIntelligenceGateway
{
    public bool ShouldFail { get; set; }
    public FailType FailType { get; set; }
    public OpportunityIntelligenceInput? LastInput { get; private set; }

    public Task<OpportunitySemanticAnalysis> AnalyzeAsync(
        OpportunityIntelligenceInput input,
        CancellationToken cancellationToken)
    {
        LastInput = input;

        if (ShouldFail)
        {
            return FailType switch
            {
                FailType.Unavailable =>
                    throw new Application.Exceptions.OpportunityIntelligenceUnavailableException("Provider unavailable"),
                FailType.InvalidResponse =>
                    throw new Application.Exceptions.OpportunityIntelligenceInvalidResponseException("Invalid response"),
                _ => throw new NotImplementedException()
            };
        }

        return Task.FromResult(new OpportunitySemanticAnalysis
        {
            EstimatedHours = 80,
            DeliveryRisk = DeliveryRiskLevel.Low,
            AiLeverage = AiLeverageLevel.VeryHigh,
            Confidence = AnalysisConfidence.High,
            TechnicalRequirements = ["C#", "ASP.NET Core"],
            RiskFactors = ["No specific risks identified"],
            AiLeverageReasons = ["Standard CRUD operations"],
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

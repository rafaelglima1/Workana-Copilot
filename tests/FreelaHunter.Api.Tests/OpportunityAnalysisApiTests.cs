using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Application;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Api.Tests;

[Collection("Integration")]
public class OpportunityAnalysisApiTests
{
    [Fact]
    public async Task AnalyzeOpportunity_Existing_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = CreateClientWithFakeGateway(factory);
        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_ReturnsValidStructure()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = CreateClientWithFakeGateway(factory);
        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync(
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
        await using var factory = new TestWebApplicationFactory();
        using var client = CreateClientWithFakeGateway(factory);

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{Guid.NewGuid()}/analyze", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_InvalidId_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = CreateClientWithFakeGateway(factory);

        var response = await client.PostAsJsonAsync(
            "/api/opportunities/00000000-0000-0000-0000-000000000000/analyze", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_ProviderFailure_Returns503()
    {
        await using var factory = new TestWebApplicationFactory();
        var fakeGateway = new FakeAnalysisGateway { ShouldFail = true, FailType = FailType.Unavailable };
        using var client = CreateClientWithFakeGateway(factory, fakeGateway);
        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_InvalidProviderResponse_Returns502()
    {
        await using var factory = new TestWebApplicationFactory();
        var fakeGateway = new FakeAnalysisGateway { ShouldFail = true, FailType = FailType.InvalidResponse };
        using var client = CreateClientWithFakeGateway(factory, fakeGateway);
        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task GetHealth_StillReturns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AnalyzeOpportunity_InputFieldsCorrect()
    {
        await using var factory = new TestWebApplicationFactory();
        var fakeGateway = new FakeAnalysisGateway();
        using var client = CreateClientWithFakeGateway(factory, fakeGateway);
        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/analyze", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(fakeGateway.LastInput);
        Assert.Equal("Test CRUD Project", fakeGateway.LastInput!.Title);
    }

    [Fact]
    public async Task AnalyzeOpportunity_ReturnsUsageMetadata()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = CreateClientWithFakeGateway(factory);
        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var response = await client.PostAsJsonAsync(
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

    private static HttpClient CreateClientWithFakeGateway(
        TestWebApplicationFactory factory,
        FakeAnalysisGateway? fakeGateway = null)
    {
        fakeGateway ??= new FakeAnalysisGateway();
        factory.FakeGateway = fakeGateway;
        return factory.CreateClient();
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

using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class AnalyzeOpportunityTests
{
    private readonly FakeOpportunityRepository _repository = new();
    private readonly FakeIntelligenceGateway _gateway = new();

    private AnalyzeOpportunityHandler CreateHandler() =>
        new(_repository, _gateway);

    private async Task<Opportunity> CreateAndStoreOpportunity()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test CRUD API",
            description: "Build a REST API with ASP.NET Core and PostgreSQL",
            budget: Budget.Exact(10000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject),
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["C#", "ASP.NET Core", "PostgreSQL"],
            category: "Web Development",
            subcategory: "API");

        await _repository.AddAsync(opportunity, CancellationToken.None);
        return opportunity;
    }

    [Fact]
    public async Task ExistingOpportunity_ReturnsAnalysis()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(120, result!.EstimatedHours);
        Assert.Equal(DeliveryRiskLevel.Moderate, result.DeliveryRisk);
        Assert.Equal(AiLeverageLevel.High, result.AiLeverage);
        Assert.Equal(AnalysisConfidence.Medium, result.Confidence);
    }

    [Fact]
    public async Task NonExistingOpportunity_ReturnsNull()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task NonExistingOpportunity_GatewayNotCalled()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(_gateway.Inputs);
    }

    [Fact]
    public async Task MapsTitle()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Equal("Test CRUD API", _gateway.Inputs.Single().Title);
    }

    [Fact]
    public async Task MapsDescription()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Contains("REST API", _gateway.Inputs.Single().Description);
    }

    [Fact]
    public async Task MapsMarketplace()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Equal("Workana", _gateway.Inputs.Single().Marketplace);
    }

    [Fact]
    public async Task MapsBudgetSummary()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Contains("BRL", _gateway.Inputs.Single().BudgetSummary!);
        Assert.Contains("FixedProject", _gateway.Inputs.Single().BudgetSummary!);
    }

    [Fact]
    public async Task MapsSkills()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        var skills = _gateway.Inputs.Single().RequiredSkills;
        Assert.Equal(3, skills.Count);
        Assert.Contains("C#", skills);
        Assert.Contains("ASP.NET Core", skills);
        Assert.Contains("PostgreSQL", skills);
    }

    [Fact]
    public async Task MapsClientSummary()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Contains("5 published", _gateway.Inputs.Single().ClientSummary!);
        Assert.Contains("5 paid", _gateway.Inputs.Single().ClientSummary!);
    }

    [Fact]
    public async Task MapsCompetitionSummary()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Contains("10 proposals", _gateway.Inputs.Single().CompetitionSummary!);
    }

    [Fact]
    public async Task MapsCategory()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Equal("Web Development", _gateway.Inputs.Single().Category);
    }

    [Fact]
    public async Task ResultIsUnchanged()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(120, result!.EstimatedHours);
        Assert.Equal(DeliveryRiskLevel.Moderate, result.DeliveryRisk);
        Assert.Equal(AiLeverageLevel.High, result.AiLeverage);
        Assert.Equal(AnalysisConfidence.Medium, result.Confidence);
        Assert.Equal(2, result.TechnicalRequirements.Count);
        Assert.Single(result.RiskFactors);
        Assert.Single(result.AiLeverageReasons);
        Assert.Single(result.ClarifyingQuestions);
    }

    [Fact]
    public async Task OpportunityNotModified()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        var stored = await _repository.GetByIdAsync(opportunity.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("Test CRUD API", stored!.Title);
        Assert.Equal(OpportunityStatus.New, stored.Status);
    }

    [Fact]
    public async Task ScorerNotExecuted()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var handler = CreateHandler();

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.DoesNotContain("Overall", result!.GetType().GetProperties().Select(p => p.Name));
    }
}

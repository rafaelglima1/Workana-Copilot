using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class ScoreOpportunityTests
{
    private readonly FakeOpportunityRepository _repository = new();
    private readonly ScoreOpportunity _handler;

    public ScoreOpportunityTests()
    {
        _handler = new ScoreOpportunity(_repository);
    }

    private async Task<Opportunity> CreateAndStoreOpportunity()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Exact(10000, CurrencyCode.From("BRL"), BudgetBasis.Hourly),
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["C#"]);

        await _repository.AddAsync(opportunity, CancellationToken.None);
        return opportunity;
    }

    [Fact]
    public async Task ExistingOpportunity_ReturnsScore()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(opportunity.Id.Value, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.InRange(result!.Overall, 0, 100);
        Assert.False(string.IsNullOrEmpty(result.Recommendation));
    }

    [Fact]
    public async Task NonExistingOpportunity_ReturnsNull()
    {
        var result = await _handler.HandleAsync(
            Guid.NewGuid(),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task MapsSkills()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var request = CreateValidRequest();
        request.Skills = [new SkillInput { Name = "C#", Level = ContractSkillLevel.Expert }];

        var result = await _handler.HandleAsync(opportunity.Id.Value, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.TechnicalFit.Score);
    }

    [Fact]
    public async Task MapsTargetRate()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var request = CreateValidRequest();
        request.TargetHourlyRate = new RateTargetInput { Amount = 100, Currency = "BRL" };

        var result = await _handler.HandleAsync(opportunity.Id.Value, request, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task MapsRisk()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var request = CreateValidRequest();
        request.DeliveryRisk = ContractDeliveryRisk.Low;

        var result = await _handler.HandleAsync(opportunity.Id.Value, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.DeliveryConfidence.Score);
    }

    [Fact]
    public async Task MapsAiLeverage()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var request = CreateValidRequest();
        request.AiLeverage = ContractAiLeverage.VeryHigh;

        var result = await _handler.HandleAsync(opportunity.Id.Value, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100, result.AiLeverage.Score);
    }

    [Fact]
    public async Task ReturnsCompleteBreakdown()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(opportunity.Id.Value, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.TechnicalFit);
        Assert.NotNull(result.EconomicPotential);
        Assert.NotNull(result.ClientQuality);
        Assert.NotNull(result.Competition);
        Assert.NotNull(result.DeliveryConfidence);
        Assert.NotNull(result.AiLeverage);
        Assert.NotEmpty(result.TechnicalFit.Rules);
    }

    [Fact]
    public async Task DoesNotPersistScore()
    {
        var opportunity = await CreateAndStoreOpportunity();
        var request = CreateValidRequest();

        await _handler.HandleAsync(opportunity.Id.Value, request, CancellationToken.None);

        var retrieved = await _repository.GetByIdAsync(opportunity.Id, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(opportunity.Id.Value, retrieved!.Id.Value);
    }

    private static ScoreOpportunityRequest CreateValidRequest()
    {
        return new ScoreOpportunityRequest
        {
            Skills = [new SkillInput { Name = "C#", Level = ContractSkillLevel.Expert }],
            TargetHourlyRate = new RateTargetInput { Amount = 100, Currency = "BRL" },
            DeliveryRisk = ContractDeliveryRisk.Moderate,
            AiLeverage = ContractAiLeverage.High
        };
    }
}

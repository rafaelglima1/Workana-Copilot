using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class OpportunityScorerEconomicTests
{
    private static Opportunity CreateOpportunity(Budget budget)
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: budget,
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);
    }

    private static OpportunityScoringContext CreateContext(
        decimal targetRate = 100,
        string currency = "BRL",
        decimal? estimatedHours = null)
    {
        return OpportunityScoringContext.Create(
            [],
            HourlyRateTarget.Create(targetRate, CurrencyCode.From(currency)),
            estimatedHours,
            DeliveryRiskLevel.Moderate,
            AiLeverageLevel.None);
    }

    [Fact]
    public void Unknown_Returns50()
    {
        var opportunity = CreateOpportunity(Budget.Unknown());
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.EconomicPotential.Score.Value);
    }

    [Fact]
    public void Negotiable_Returns50()
    {
        var opportunity = CreateOpportunity(Budget.Negotiable(CurrencyCode.From("BRL")));
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.EconomicPotential.Score.Value);
    }

    [Fact]
    public void Monthly_Returns50()
    {
        var opportunity = CreateOpportunity(
            Budget.Range(5000, 10000, CurrencyCode.From("BRL"), BudgetBasis.Monthly));
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.EconomicPotential.Score.Value);
    }

    [Fact]
    public void CurrencyMismatch_Returns50()
    {
        var opportunity = CreateOpportunity(
            Budget.Exact(5000, CurrencyCode.From("USD")));
        var score = OpportunityScorer.Score(opportunity, CreateContext(currency: "BRL"));

        Assert.Equal(50, score.EconomicPotential.Score.Value);
    }

    [Theory]
    [InlineData(1.50, 100)]
    [InlineData(1.25, 90)]
    [InlineData(1.00, 80)]
    [InlineData(0.80, 60)]
    [InlineData(0.60, 40)]
    [InlineData(0.40, 20)]
    [InlineData(0.39, 5)]
    public void FixedProject_RatioBoundaries(decimal ratio, int expected)
    {
        var amount = 100 * ratio * 120;
        var opportunity = CreateOpportunity(
            Budget.Exact(amount, CurrencyCode.From("BRL"), BudgetBasis.FixedProject));
        var score = OpportunityScorer.Score(opportunity, CreateContext(estimatedHours: 120));

        Assert.Equal(expected, score.EconomicPotential.Score.Value);
    }

    [Theory]
    [InlineData(1.50, 100)]
    [InlineData(1.25, 90)]
    [InlineData(1.00, 80)]
    [InlineData(0.80, 60)]
    [InlineData(0.60, 40)]
    [InlineData(0.40, 20)]
    [InlineData(0.39, 5)]
    public void Hourly_RatioBoundaries(decimal ratio, int expected)
    {
        var amount = 100 * ratio;
        var opportunity = CreateOpportunity(
            Budget.Exact(amount, CurrencyCode.From("BRL"), BudgetBasis.Hourly));
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(expected, score.EconomicPotential.Score.Value);
    }

    [Fact]
    public void Range_UsesMaximum()
    {
        var opportunity = CreateOpportunity(
            Budget.Range(500, 12000, CurrencyCode.From("BRL"), BudgetBasis.Hourly));
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        // Ratio = 12000/100 = 120 -> >= 1.50 -> 100
        Assert.Equal(100, score.EconomicPotential.Score.Value);
    }

    [Fact]
    public void UpTo_UsesMaximum()
    {
        var opportunity = CreateOpportunity(
            Budget.UpTo(12000, CurrencyCode.From("BRL"), BudgetBasis.Hourly));
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(100, score.EconomicPotential.Score.Value);
    }

    [Fact]
    public void From_UsesMinimum()
    {
        var opportunity = CreateOpportunity(
            Budget.From(12000, CurrencyCode.From("BRL"), BudgetBasis.Hourly));
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(100, score.EconomicPotential.Score.Value);
    }

    [Fact]
    public void FixedProject_MissingEstimatedHours_Throws()
    {
        var opportunity = CreateOpportunity(
            Budget.Exact(5000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject));

        Assert.Throws<DomainException>(() =>
            OpportunityScorer.Score(opportunity, CreateContext()));
    }
}

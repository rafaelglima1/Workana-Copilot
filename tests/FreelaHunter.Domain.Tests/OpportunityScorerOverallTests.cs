using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class OpportunityScorerOverallTests
{
    private static Opportunity CreateOpportunity(
        params string[] requiredSkills)
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Exact(10000, CurrencyCode.From("BRL"), BudgetBasis.Hourly),
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: requiredSkills);
    }

    private static OpportunityScoringContext CreateContext(
        SkillLevel skillLevel = SkillLevel.Expert,
        DeliveryRiskLevel risk = DeliveryRiskLevel.Low,
        AiLeverageLevel ai = AiLeverageLevel.High)
    {
        return OpportunityScoringContext.Create(
            [SkillCapability.Create("C#", skillLevel)],
            HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
            null,
            risk,
            ai);
    }

    [Fact]
    public void Overall_AlwaysBetween0And100()
    {
        var opportunity = CreateOpportunity("C#");
        var context = CreateContext();

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.InRange(score.Overall.Value, 0, 100);
    }

    [Fact]
    public void SameInputs_ProduceSameResult()
    {
        var opportunity = CreateOpportunity("C#");
        var context = CreateContext();

        var score1 = OpportunityScorer.Score(opportunity, context);
        var score2 = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(score1.Overall.Value, score2.Overall.Value);
        Assert.Equal(score1.Recommendation, score2.Recommendation);
    }

    [Fact]
    public void CustomWeights_Valid()
    {
        var opportunity = CreateOpportunity("C#");
        var context = CreateContext();
        var weights = ScoringWeights.Create(50, 20, 10, 10, 5, 5);

        var score = OpportunityScorer.Score(opportunity, context, weights);

        Assert.InRange(score.Overall.Value, 0, 100);
    }

    [Fact]
    public void InvalidWeights_Throws()
    {
        Assert.Throws<DomainException>(() =>
            ScoringWeights.Create(50, 20, 10, 10, 5, 4));
    }

    [Theory]
    [InlineData(49.99, OpportunityRecommendation.Skip)]
    [InlineData(50, OpportunityRecommendation.ConsiderWithCaution)]
    [InlineData(64.99, OpportunityRecommendation.ConsiderWithCaution)]
    [InlineData(65, OpportunityRecommendation.Apply)]
    [InlineData(79.99, OpportunityRecommendation.Apply)]
    [InlineData(80, OpportunityRecommendation.Prioritize)]
    [InlineData(100, OpportunityRecommendation.Prioritize)]
    public void Recommendation_Thresholds(decimal overall, OpportunityRecommendation expected)
    {
        var score = ScoreValue.From(overall);
        var recommendation = score.Value switch
        {
            >= 80 => OpportunityRecommendation.Prioritize,
            >= 65 => OpportunityRecommendation.Apply,
            >= 50 => OpportunityRecommendation.ConsiderWithCaution,
            _ => OpportunityRecommendation.Skip
        };

        Assert.Equal(expected, recommendation);
    }
}

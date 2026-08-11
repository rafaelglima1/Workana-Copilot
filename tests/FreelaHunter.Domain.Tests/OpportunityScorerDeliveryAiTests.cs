using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class OpportunityScorerDeliveryAiTests
{
    private static Opportunity CreateOpportunity()
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);
    }

    private static OpportunityScoringContext CreateContext(DeliveryRiskLevel risk, AiLeverageLevel ai)
    {
        return OpportunityScoringContext.Create(
            [],
            HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
            null,
            risk,
            ai);
    }

    [Theory]
    [InlineData(DeliveryRiskLevel.Low, 100)]
    [InlineData(DeliveryRiskLevel.Moderate, 75)]
    [InlineData(DeliveryRiskLevel.High, 40)]
    [InlineData(DeliveryRiskLevel.Critical, 10)]
    public void DeliveryRisk_MapsCorrectly(DeliveryRiskLevel risk, int expected)
    {
        var score = OpportunityScorer.Score(CreateOpportunity(), CreateContext(risk, AiLeverageLevel.None));

        Assert.Equal(expected, score.DeliveryConfidence.Score.Value);
    }

    [Theory]
    [InlineData(AiLeverageLevel.None, 0)]
    [InlineData(AiLeverageLevel.Low, 25)]
    [InlineData(AiLeverageLevel.Medium, 50)]
    [InlineData(AiLeverageLevel.High, 75)]
    [InlineData(AiLeverageLevel.VeryHigh, 100)]
    public void AiLeverage_MapsCorrectly(AiLeverageLevel ai, int expected)
    {
        var score = OpportunityScorer.Score(CreateOpportunity(), CreateContext(DeliveryRiskLevel.Moderate, ai));

        Assert.Equal(expected, score.AiLeverage.Score.Value);
    }
}

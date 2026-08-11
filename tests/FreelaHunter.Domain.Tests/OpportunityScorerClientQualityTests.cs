using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class OpportunityScorerClientQualityTests
{
    private static Opportunity CreateOpportunity(ClientSnapshot client)
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: client,
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);
    }

    private static OpportunityScoringContext CreateContext()
    {
        return OpportunityScoringContext.Create(
            [],
            HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
            null,
            DeliveryRiskLevel.Moderate,
            AiLeverageLevel.None);
    }

    [Fact]
    public void NewClient_Returns50()
    {
        var opportunity = CreateOpportunity(ClientSnapshot.Create());
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.ClientQuality.Score.Value);
    }

    [Fact]
    public void UnknownData_Returns50()
    {
        var client = ClientSnapshot.Create(
            displayName: "Client",
            country: "BR",
            publishedProjects: null,
            paidProjects: null,
            rating: null);

        var opportunity = CreateOpportunity(client);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.ClientQuality.Score.Value);
    }

    [Fact]
    public void ZeroPublished_Returns50()
    {
        var client = ClientSnapshot.Create(
            publishedProjects: 0,
            paidProjects: 0);

        var opportunity = CreateOpportunity(client);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.ClientQuality.Score.Value);
    }

    [Fact]
    public void AllPaid_Returns94()
    {
        var client = ClientSnapshot.Create(
            publishedProjects: 10,
            paidProjects: 10,
            rating: 5);

        var opportunity = CreateOpportunity(client);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(100, score.ClientQuality.Score.Value);
    }

    [Fact]
    public void LowPaymentRatio()
    {
        var client = ClientSnapshot.Create(
            publishedProjects: 10,
            paidProjects: 1);

        var opportunity = CreateOpportunity(client);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(22, score.ClientQuality.Score.Value);
    }

    [Fact]
    public void RatingZero_Returns35()
    {
        var client = ClientSnapshot.Create(rating: 0);

        var opportunity = CreateOpportunity(client);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        // PaymentHistory=35 (unknown) + RatingComponent=0 = 35
        Assert.Equal(35, score.ClientQuality.Score.Value);
    }

    [Fact]
    public void RatingFive_ReturnsHigh()
    {
        var client = ClientSnapshot.Create(
            publishedProjects: 5,
            paidProjects: 5,
            rating: 5);

        var opportunity = CreateOpportunity(client);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(100, score.ClientQuality.Score.Value);
    }

    [Fact]
    public void RatingNull_Returns15()
    {
        var client = ClientSnapshot.Create(rating: null);

        var opportunity = CreateOpportunity(client);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.ClientQuality.Score.Value);
    }
}

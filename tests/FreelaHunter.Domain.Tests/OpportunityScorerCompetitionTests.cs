using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class OpportunityScorerCompetitionTests
{
    private static Opportunity CreateOpportunity(int? proposalCount = null, int? interestedCount = null)
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(proposalCount, interestedCount),
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

    [Theory]
    [InlineData(5, 100)]
    [InlineData(6, 90)]
    [InlineData(10, 90)]
    [InlineData(11, 75)]
    [InlineData(20, 75)]
    [InlineData(21, 55)]
    [InlineData(40, 55)]
    [InlineData(41, 35)]
    [InlineData(60, 35)]
    [InlineData(61, 20)]
    [InlineData(100, 20)]
    [InlineData(101, 10)]
    public void ProposalCount_Boundaries(int proposals, int expected)
    {
        var opportunity = CreateOpportunity(proposalCount: proposals);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(expected, score.Competition.Score.Value);
    }

    [Theory]
    [InlineData(10, 100)]
    [InlineData(11, 80)]
    [InlineData(25, 80)]
    [InlineData(26, 60)]
    [InlineData(50, 60)]
    [InlineData(51, 35)]
    [InlineData(100, 35)]
    [InlineData(101, 15)]
    public void InterestedCount_Boundaries(int interested, int expected)
    {
        var opportunity = CreateOpportunity(interestedCount: interested);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(expected, score.Competition.Score.Value);
    }

    [Fact]
    public void BothNull_Returns50()
    {
        var opportunity = CreateOpportunity();
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(50, score.Competition.Score.Value);
    }

    [Fact]
    public void OnlyProposal_UsesProposalScore()
    {
        var opportunity = CreateOpportunity(proposalCount: 5);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(100, score.Competition.Score.Value);
    }

    [Fact]
    public void OnlyInterested_UsesInterestedScore()
    {
        var opportunity = CreateOpportunity(interestedCount: 10);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        Assert.Equal(100, score.Competition.Score.Value);
    }

    [Fact]
    public void BothPresent_UsesWeightedAverage()
    {
        var opportunity = CreateOpportunity(proposalCount: 54, interestedCount: 50);
        var score = OpportunityScorer.Score(opportunity, CreateContext());

        // 35 * 0.70 + 60 * 0.30 = 24.5 + 18 = 42.5
        Assert.Equal(42.50m, score.Competition.Score.Value);
    }
}

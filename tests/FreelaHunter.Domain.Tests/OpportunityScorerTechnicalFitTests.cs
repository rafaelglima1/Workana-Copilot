using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class OpportunityScorerTechnicalFitTests
{
    private static Opportunity CreateOpportunity(params string[] requiredSkills)
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: requiredSkills);
    }

    private static OpportunityScoringContext CreateContext(params SkillCapability[] skills)
    {
        return OpportunityScoringContext.Create(
            skills,
            HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
            null,
            DeliveryRiskLevel.Moderate,
            AiLeverageLevel.None);
    }

    [Fact]
    public void NoRequiredSkills_Returns50()
    {
        var opportunity = CreateOpportunity();
        var context = CreateContext();

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(50, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void AllExpert_Returns100()
    {
        var opportunity = CreateOpportunity("C#", "API");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("API", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void AllMissing_Returns0()
    {
        var opportunity = CreateOpportunity("C#", "API");
        var context = CreateContext();

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(0, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void MixedSkills_CalculatesAverage()
    {
        var opportunity = CreateOpportunity("C#", "API", "Flutter");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("API", SkillLevel.Expert),
            SkillCapability.Create("Flutter", SkillLevel.None));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(66.67m, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void CaseInsensitiveMatch()
    {
        var opportunity = CreateOpportunity("c#");
        var context = CreateContext(SkillCapability.Create("C#", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void ExtraSkillsInProfile_DontAffectResult()
    {
        var opportunity = CreateOpportunity("C#");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("Flutter", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }
}

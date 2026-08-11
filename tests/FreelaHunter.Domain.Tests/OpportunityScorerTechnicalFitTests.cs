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

    [Fact]
    public void AliasMatch_RecognizesCanonicalEquivalent()
    {
        var opportunity = CreateOpportunity("CSharp");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void AliasMatch_ReactJsMatchesReact()
    {
        var opportunity = CreateOpportunity("React.js");
        var context = CreateContext(
            SkillCapability.Create("React", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void AliasMatch_K8sMatchesKubernetes()
    {
        var opportunity = CreateOpportunity("K8s");
        var context = CreateContext(
            SkillCapability.Create("Kubernetes", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void AliasMatch_PostgresMatchesPostgreSQL()
    {
        var opportunity = CreateOpportunity("Postgres");
        var context = CreateContext(
            SkillCapability.Create("PostgreSQL", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void AliasMatch_ExactPreferredOverAlias()
    {
        var opportunity = CreateOpportunity("C#");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("CSharp", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(100, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void AliasMatch_MixedExactAndAlias()
    {
        var opportunity = CreateOpportunity("C#", "React.js", "Python");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("React", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Equal(66.67m, score.TechnicalFit.Score.Value);
    }

    [Fact]
    public void TechnicalSkillMatches_ContainsExactMatch()
    {
        var opportunity = CreateOpportunity("C#");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Single(score.TechnicalSkillMatches);
        Assert.Equal(SkillMatchKind.Exact, score.TechnicalSkillMatches.First().MatchKind);
    }

    [Fact]
    public void TechnicalSkillMatches_ContainsAliasMatch()
    {
        var opportunity = CreateOpportunity("CSharp");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Single(score.TechnicalSkillMatches);
        Assert.Equal(SkillMatchKind.Alias, score.TechnicalSkillMatches.First().MatchKind);
    }

    [Fact]
    public void TechnicalSkillMatches_ContainsNoMatch()
    {
        var opportunity = CreateOpportunity("Python");
        var context = CreateContext(
            SkillCapability.Create("C#", SkillLevel.Expert));

        var score = OpportunityScorer.Score(opportunity, context);

        Assert.Single(score.TechnicalSkillMatches);
        Assert.Equal(SkillMatchKind.NoMatch, score.TechnicalSkillMatches.First().MatchKind);
    }
}

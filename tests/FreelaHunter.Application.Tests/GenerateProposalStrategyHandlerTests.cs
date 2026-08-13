using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class GenerateProposalStrategyHandlerTests
{
    private readonly FakeOpportunityRepository _opportunityRepository = new();
    private readonly FakeFreelancerProfileRepository _profileRepository = new();
    private readonly FakeOpportunityEvaluationSnapshotRepository _snapshotRepository = new();
    private readonly GenerateProposalStrategyHandler _handler;

    public GenerateProposalStrategyHandlerTests()
    {
        _handler = new GenerateProposalStrategyHandler(
            _opportunityRepository,
            _profileRepository,
            _snapshotRepository);
    }

    [Fact]
    public async Task HandleAsync_WithValidSnapshot_ReturnsStrategy()
    {
        var opportunity = CreateOpportunity();
        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var snapshot = CreateSnapshot(opportunity.Id.Value);
        await _snapshotRepository.SaveLatestAsync(snapshot, CancellationToken.None);

        var result = await _handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result.Decision);
        Assert.True(result.EstimatedDeliveryDays >= 0);
        Assert.True(result.EstimatedRevenue >= 0);
    }

    [Fact]
    public async Task HandleAsync_NoOpportunity_ReturnsNull()
    {
        var result = await _handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_NoProfile_ReturnsNull()
    {
        var opportunity = CreateOpportunity();
        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var result = await _handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_NoSnapshot_ReturnsNull()
    {
        var opportunity = CreateOpportunity();
        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var result = await _handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_StaleSnapshot_ReturnsNull()
    {
        var opportunity = CreateOpportunity();
        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var snapshot = CreateSnapshot(opportunity.Id.Value, isStale: true);
        await _snapshotRepository.SaveLatestAsync(snapshot, CancellationToken.None);

        var result = await _handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_HighScore_ReturnsGoDecision()
    {
        var opportunity = CreateOpportunity();
        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var snapshot = CreateSnapshot(opportunity.Id.Value, overallScore: 85m);
        await _snapshotRepository.SaveLatestAsync(snapshot, CancellationToken.None);

        var result = await _handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Go", result.Decision);
    }

    [Fact]
    public async Task HandleAsync_LowScore_ReturnsNoGoDecision()
    {
        var opportunity = CreateOpportunity();
        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var snapshot = CreateSnapshot(opportunity.Id.Value, overallScore: 30m);
        await _snapshotRepository.SaveLatestAsync(snapshot, CancellationToken.None);

        var result = await _handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("NoGo", result.Decision);
    }

    private static Opportunity CreateOpportunity()
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test Opportunity",
            description: "Description",
            budget: Budget.Exact(5000, CurrencyCode.From("BRL")),
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["C#", "API"]);
    }

    private static OpportunityEvaluationSnapshot CreateSnapshot(
        Guid opportunityId,
        decimal overallScore = 75m,
        bool isStale = false)
    {
        var scoreValue = ScoreValue.From(overallScore);
        var technicalFit = ScoreComponent.Create(scoreValue, 30, []);
        var economic = ScoreComponent.Create(scoreValue, 25, []);
        var client = ScoreComponent.Create(scoreValue, 15, []);
        var competition = ScoreComponent.Create(scoreValue, 10, []);
        var delivery = ScoreComponent.Create(scoreValue, 10, []);
        var ai = ScoreComponent.Create(scoreValue, 10, []);

        var score = OpportunityScore.Create(scoreValue, technicalFit, economic, client, competition, delivery, ai);

        var analysis = new OpportunitySemanticAnalysis
        {
            EstimatedHours = 40m,
            DeliveryRisk = DeliveryRiskLevel.Low,
            AiLeverage = AiLeverageLevel.High,
            Confidence = AnalysisConfidence.High,
            TechnicalRequirements = ["C#", "API"],
            RiskFactors = [],
            AiLeverageReasons = [],
            ClarifyingQuestions = []
        };

        var profileId = Guid.NewGuid();

        return new OpportunityEvaluationSnapshot
        {
            OpportunityId = opportunityId,
            ProfileId = profileId,
            ProfileFingerprint = "test-fingerprint",
            EvaluatedAt = DateTimeOffset.UtcNow,
            Evaluated = true,
            Recommendation = score.Recommendation,
            OverallScore = score.Overall.Value,
            Analysis = analysis,
            Score = score,
            ScoringInputs = new ScoringInputSummary
            {
                ProfileSkills = [new SkillCapabilitySummary { Name = "C#", Level = "Expert" }],
                TargetHourlyRate = new HourlyRateSummary { Amount = 100m, Currency = "BRL" },
                EstimatedHours = 40m,
                DeliveryRisk = DeliveryRiskLevel.Low,
                AiLeverage = AiLeverageLevel.High
            },
            IsStale = isStale
        };
    }
}

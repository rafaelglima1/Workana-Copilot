using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class EvaluateOpportunitySnapshotTests
{
    private readonly FakeOpportunityRepository _opportunityRepository = new();
    private readonly FakeFreelancerProfileRepository _profileRepository = new();
    private readonly FakeIntelligenceGateway _gateway = new();
    private readonly FakeOpportunityEvaluationSnapshotRepository _snapshotRepository = new();
    private static readonly TimeProvider TimeProvider = TimeProvider.System;

    private EvaluateOpportunityHandler CreateHandler() =>
        new(_opportunityRepository, _profileRepository, _gateway, _snapshotRepository, TimeProvider);

    private async Task<Opportunity> CreateAndStoreOpportunity(
        CurrencyCode? currency = null,
        IEnumerable<string>? skills = null)
    {
        var budget = currency is not null
            ? Budget.Range(5000, 15000, currency, BudgetBasis.FixedProject)
            : Budget.Range(5000, 15000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test CRUD API",
            description: "Build a REST API with ASP.NET Core",
            budget: budget,
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: skills ?? ["C#", "API"]);

        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);
        return opportunity;
    }

    private async Task StoreProfile()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);
    }

    [Fact]
    public async Task HappyPath_PersistsSnapshot()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.Single(_snapshotRepository.SavedSnapshots);
    }

    [Fact]
    public async Task HappyPath_SnapshotContainsCorrectOpportunityId()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var snapshot = _snapshotRepository.SavedSnapshots[0];
        Assert.Equal(opportunity.Id.Value, snapshot.OpportunityId);
    }

    [Fact]
    public async Task HappyPath_SnapshotIsEvaluated()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var snapshot = _snapshotRepository.SavedSnapshots[0];
        Assert.True(snapshot.Evaluated);
    }

    [Fact]
    public async Task HappyPath_SnapshotContainsScore()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var snapshot = _snapshotRepository.SavedSnapshots[0];
        Assert.InRange(snapshot.OverallScore, 0, 100);
        Assert.False(string.IsNullOrEmpty(snapshot.Recommendation.ToString()));
    }

    [Fact]
    public async Task HappyPath_SnapshotContainsAnalysis()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var snapshot = _snapshotRepository.SavedSnapshots[0];
        Assert.NotNull(snapshot.Analysis);
        Assert.Equal(120m, snapshot.Analysis.EstimatedHours);
    }

    [Fact]
    public async Task HappyPath_SnapshotContainsScoringInputs()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var snapshot = _snapshotRepository.SavedSnapshots[0];
        Assert.NotNull(snapshot.ScoringInputs);
        Assert.Equal(100m, snapshot.ScoringInputs.TargetHourlyRate.Amount);
    }

    [Fact]
    public async Task HappyPath_SnapshotContainsFingerprint()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var snapshot = _snapshotRepository.SavedSnapshots[0];
        Assert.False(string.IsNullOrEmpty(snapshot.ProfileFingerprint));
        Assert.Equal(64, snapshot.ProfileFingerprint.Length);
    }

    [Fact]
    public async Task HappyPath_SnapshotContainsEvaluatedAt()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var snapshot = _snapshotRepository.SavedSnapshots[0];
        Assert.True(snapshot.EvaluatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task NonExistingOpportunity_NoSnapshotPersisted()
    {
        await StoreProfile();

        await CreateHandler().HandleAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Empty(_snapshotRepository.SavedSnapshots);
    }

    [Fact]
    public async Task NonExistingProfile_NoSnapshotPersisted()
    {
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.Empty(_snapshotRepository.SavedSnapshots);
    }

    [Fact]
    public async Task GatewayFailure_NoSnapshotPersisted()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();
        var failingGateway = new FakeIntelligenceGateway((input, ct) =>
            throw new Exceptions.OpportunityIntelligenceUnavailableException("Provider unavailable"));
        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, failingGateway, _snapshotRepository, TimeProvider);

        try
        {
            await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);
        }
        catch
        {
            // Expected
        }

        Assert.Empty(_snapshotRepository.SavedSnapshots);
    }

    [Fact]
    public async Task Reevaluate_CallsSaveLatestTwice()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var handler = CreateHandler();
        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);
        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.Equal(2, _snapshotRepository.SavedSnapshots.Count(s => s.OpportunityId == opportunity.Id.Value));
    }

    [Fact]
    public async Task Reevaluate_LatestSnapshotIsReturned()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var handler = CreateHandler();
        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);
        await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        var retrieved = await _snapshotRepository.GetLatestAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(opportunity.Id.Value, retrieved!.OpportunityId);
    }

    [Fact]
    public async Task SnapshotCanBeRetrieved()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var retrieved = await _snapshotRepository.GetLatestAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(opportunity.Id.Value, retrieved!.OpportunityId);
        Assert.True(retrieved.Evaluated);
    }

    [Fact]
    public async Task BatchRetrieval_Works()
    {
        await StoreProfile();
        var opp1 = await CreateAndStoreOpportunity();
        var opp2 = await CreateAndStoreOpportunity(skills: ["Python"]);

        var handler = CreateHandler();
        await handler.HandleAsync(opp1.Id.Value, CancellationToken.None);
        await handler.HandleAsync(opp2.Id.Value, CancellationToken.None);

        var results = await _snapshotRepository.GetLatestBatchAsync(
            [opp1.Id.Value, opp2.Id.Value],
            CancellationToken.None);

        Assert.Equal(2, results.Count);
    }
}

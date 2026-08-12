using System.Text.Json;
using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Mappers;
using FreelaHunter.Infrastructure.Persistence.Models;
using FreelaHunter.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Tests;

public class EfOpportunityEvaluationSnapshotRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FreelaHunterDbContext _context;

    public EfOpportunityEvaluationSnapshotRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FreelaHunterDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new FreelaHunterDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private static string CreatePayloadJson()
    {
        var payload = new EfOpportunityEvaluationSnapshotRepository.SnapshotPayload
        {
            EstimatedHours = 120,
            DeliveryRisk = "Moderate",
            AiLeverage = "High",
            Confidence = "Medium",
            TechnicalRequirements = ["C#"],
            RiskFactors = [],
            AiLeverageReasons = [],
            ClarifyingQuestions = [],
            OverallScore = 75m,
            TechnicalFit = new EfOpportunityEvaluationSnapshotRepository.ScoreComponentInfo
            {
                Score = 80, Weight = 30, WeightedContribution = 24, Rules = []
            },
            EconomicPotential = new EfOpportunityEvaluationSnapshotRepository.ScoreComponentInfo
            {
                Score = 70, Weight = 25, WeightedContribution = 17.5m, Rules = []
            },
            ClientQuality = new EfOpportunityEvaluationSnapshotRepository.ScoreComponentInfo
            {
                Score = 75, Weight = 20, WeightedContribution = 15, Rules = []
            },
            Competition = new EfOpportunityEvaluationSnapshotRepository.ScoreComponentInfo
            {
                Score = 60, Weight = 10, WeightedContribution = 6, Rules = []
            },
            DeliveryConfidence = new EfOpportunityEvaluationSnapshotRepository.ScoreComponentInfo
            {
                Score = 75, Weight = 10, WeightedContribution = 7.5m, Rules = []
            },
            AiLeverageScore = new EfOpportunityEvaluationSnapshotRepository.ScoreComponentInfo
            {
                Score = 80, Weight = 5, WeightedContribution = 4, Rules = []
            },
            TargetHourlyRateAmount = 100,
            TargetHourlyRateCurrency = "BRL"
        };

        return JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    private async Task<Guid> CreateOpportunity(string title = "Test")
    {
        var opportunity = Opportunity.Rehydrate(
            id: OpportunityId.New(),
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            externalReference: null,
            title: title,
            description: "Description",
            category: null,
            subcategory: null,
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL")),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            requiredSkills: ["C#"],
            publishedAt: DateTimeOffset.UtcNow,
            clientDeadline: null,
            capturedAt: DateTimeOffset.UtcNow,
            status: OpportunityStatus.New);

        var record = OpportunityPersistenceMapper.ToRecord(opportunity);
        await _context.Opportunities.AddAsync(record);
        await _context.SaveChangesAsync();
        return opportunity.Id.Value;
    }

    private static OpportunityEvaluationSnapshot CreateSnapshot(
        Guid opportunityId,
        bool evaluated = true,
        string? fingerprint = null)
    {
        return new OpportunityEvaluationSnapshot
        {
            OpportunityId = opportunityId,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = fingerprint ?? "abc123",
            EvaluatedAt = DateTimeOffset.UtcNow,
            Evaluated = evaluated,
            Recommendation = OpportunityRecommendation.Apply,
            OverallScore = 75m,
            Analysis = new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["C#"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = []
            },
            Score = Domain.ValueObjects.OpportunityScore.Create(
                ScoreValue.From(75),
                Domain.ValueObjects.ScoreComponent.Create(ScoreValue.From(80), 30, []),
                Domain.ValueObjects.ScoreComponent.Create(ScoreValue.From(70), 25, []),
                Domain.ValueObjects.ScoreComponent.Create(ScoreValue.From(75), 20, []),
                Domain.ValueObjects.ScoreComponent.Create(ScoreValue.From(60), 10, []),
                Domain.ValueObjects.ScoreComponent.Create(ScoreValue.From(75), 10, []),
                Domain.ValueObjects.ScoreComponent.Create(ScoreValue.From(80), 5, [])),
            ScoringInputs = new ScoringInputSummary
            {
                ProfileSkills = [],
                TargetHourlyRate = new HourlyRateSummary { Amount = 100, Currency = "BRL" },
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High
            },
            IsStale = false
        };
    }

    [Fact]
    public async Task SaveLatestAsync_InsertsNewRecord()
    {
        var opportunityId = await CreateOpportunity();
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);
        var snapshot = CreateSnapshot(opportunityId);

        await repo.SaveLatestAsync(snapshot, CancellationToken.None);

        var stored = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .FirstOrDefaultAsync(x => x.OpportunityId == opportunityId);
        Assert.NotNull(stored);
        Assert.True(stored.Evaluated);
        Assert.Equal(75m, stored.OverallScore);
        Assert.Equal("Apply", stored.Recommendation);
    }

    [Fact]
    public async Task SaveLatestAsync_UpsertsExistingRecord()
    {
        var opportunityId = await CreateOpportunity();
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);

        var snapshot1 = CreateSnapshot(opportunityId, fingerprint: "fp1");
        await repo.SaveLatestAsync(snapshot1, CancellationToken.None);

        var snapshot2 = CreateSnapshot(opportunityId, fingerprint: "fp2");
        snapshot2 = snapshot2 with { OverallScore = 85m };
        await repo.SaveLatestAsync(snapshot2, CancellationToken.None);

        var count = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .CountAsync(x => x.OpportunityId == opportunityId);
        Assert.Equal(1, count);

        var stored = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .FirstAsync(x => x.OpportunityId == opportunityId);
        Assert.Equal("fp2", stored.ProfileFingerprint);
        Assert.Equal(85m, stored.OverallScore);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNull_WhenNoSnapshot()
    {
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);

        var result = await repo.GetLatestAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsSnapshot_WhenExists()
    {
        var opportunityId = await CreateOpportunity();
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);
        var snapshot = CreateSnapshot(opportunityId);

        await repo.SaveLatestAsync(snapshot, CancellationToken.None);

        var result = await repo.GetLatestAsync(opportunityId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(opportunityId, result!.OpportunityId);
        Assert.Equal(75m, result.OverallScore);
        Assert.Equal(OpportunityRecommendation.Apply, result.Recommendation);
    }

    [Fact]
    public async Task GetLatestBatchAsync_ReturnsEmpty_WhenEmptyIds()
    {
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);

        var result = await repo.GetLatestBatchAsync([], CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLatestBatchAsync_ReturnsMatchingSnapshots()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);

        await repo.SaveLatestAsync(CreateSnapshot(opp1), CancellationToken.None);
        await repo.SaveLatestAsync(CreateSnapshot(opp2), CancellationToken.None);

        var results = await repo.GetLatestBatchAsync([opp1, opp2], CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task GetLatestBatchAsync_OnlyReturnsRequested()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var opp3 = await CreateOpportunity("Three");
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);

        await repo.SaveLatestAsync(CreateSnapshot(opp1), CancellationToken.None);
        await repo.SaveLatestAsync(CreateSnapshot(opp2), CancellationToken.None);

        var results = await repo.GetLatestBatchAsync([opp1, opp3], CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(opp1, results.First().OpportunityId);
    }

    [Fact]
    public async Task SaveLatestAsync_PayloadSchemaVersionIsOne()
    {
        var opportunityId = await CreateOpportunity();
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);
        var snapshot = CreateSnapshot(opportunityId);

        await repo.SaveLatestAsync(snapshot, CancellationToken.None);

        var stored = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .FirstAsync(x => x.OpportunityId == opportunityId);
        Assert.Equal(1, stored.PayloadSchemaVersion);
    }

    [Fact]
    public async Task SaveLatestAsync_PayloadJsonContainsAnalysisAndScore()
    {
        var opportunityId = await CreateOpportunity();
        var repo = new EfOpportunityEvaluationSnapshotRepository(_context);
        var snapshot = CreateSnapshot(opportunityId);

        await repo.SaveLatestAsync(snapshot, CancellationToken.None);

        var stored = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .FirstAsync(x => x.OpportunityId == opportunityId);
        Assert.Contains("estimatedHours", stored.PayloadJson);
        Assert.Contains("overallScore", stored.PayloadJson);
        Assert.Contains("targetHourlyRateAmount", stored.PayloadJson);
    }
}

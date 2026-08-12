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

public class DatabaseSideEvaluationQueryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FreelaHunterDbContext _context;

    public DatabaseSideEvaluationQueryTests()
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

    private async Task<Guid> CreateOpportunity(string title = "Test", DateTimeOffset? capturedAt = null)
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
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            requiredSkills: ["C#"],
            publishedAt: DateTimeOffset.UtcNow,
            clientDeadline: null,
            capturedAt: capturedAt ?? DateTimeOffset.UtcNow,
            status: OpportunityStatus.New);

        var record = OpportunityPersistenceMapper.ToRecord(opportunity);
        await _context.Opportunities.AddAsync(record);
        await _context.SaveChangesAsync();
        return opportunity.Id.Value;
    }

    private async Task CreateSnapshot(
        Guid opportunityId,
        bool evaluated = true,
        string recommendation = "Apply",
        decimal overallScore = 75m,
        string fingerprint = "abc123")
    {
        var record = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opportunityId,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = fingerprint,
            EvaluatedAtUtc = DateTime.UtcNow,
            Evaluated = evaluated,
            Recommendation = recommendation,
            OverallScore = overallScore,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };

        _context.Set<OpportunityEvaluationSnapshotRecord>().Add(record);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task SearchAsync_EvaluatedFilter_EvaluatedOnly()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        await CreateSnapshot(opp1, evaluated: true);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { Evaluated = true };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(opp1, result.Items.First().Id);
    }

    [Fact]
    public async Task SearchAsync_EvaluatedFilter_UnevaluatedOnly()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        await CreateSnapshot(opp1, evaluated: true);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { Evaluated = false };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(opp2, result.Items.First().Id);
    }

    [Fact]
    public async Task SearchAsync_RecommendationFilter()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var opp3 = await CreateOpportunity("Three");
        await CreateSnapshot(opp1, recommendation: "Prioritize", overallScore: 90m);
        await CreateSnapshot(opp2, recommendation: "Apply", overallScore: 70m);
        await CreateSnapshot(opp3, recommendation: "Skip", overallScore: 30m);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { Recommendation = OpportunityRecommendation.Prioritize };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(opp1, result.Items.First().Id);
    }

    [Fact]
    public async Task SearchAsync_MinScoreFilter()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        await CreateSnapshot(opp1, overallScore: 90m);
        await CreateSnapshot(opp2, overallScore: 50m);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { MinScore = 80m };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(opp1, result.Items.First().Id);
    }

    [Fact]
    public async Task SearchAsync_MaxScoreFilter()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        await CreateSnapshot(opp1, overallScore: 90m);
        await CreateSnapshot(opp2, overallScore: 50m);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { MaxScore = 60m };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(opp2, result.Items.First().Id);
    }

    [Fact]
    public async Task SearchAsync_ScoreRangeFilter()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var opp3 = await CreateOpportunity("Three");
        await CreateSnapshot(opp1, overallScore: 90m);
        await CreateSnapshot(opp2, overallScore: 60m);
        await CreateSnapshot(opp3, overallScore: 30m);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { MinScore = 40m, MaxScore = 70m };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(opp2, result.Items.First().Id);
    }

    [Fact]
    public async Task SearchAsync_OverallAscSort()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var opp3 = await CreateOpportunity("Three");
        await CreateSnapshot(opp1, overallScore: 90m);
        await CreateSnapshot(opp2, overallScore: 50m);
        await CreateSnapshot(opp3, overallScore: 70m);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { Sort = OpportunitySort.OverallAsc };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);
        var items = result.Items.ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal(opp2, items[0].Id);
        Assert.Equal(opp3, items[1].Id);
        Assert.Equal(opp1, items[2].Id);
    }

    [Fact]
    public async Task SearchAsync_OverallDescSort()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var opp3 = await CreateOpportunity("Three");
        await CreateSnapshot(opp1, overallScore: 90m);
        await CreateSnapshot(opp2, overallScore: 50m);
        await CreateSnapshot(opp3, overallScore: 70m);

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria { Sort = OpportunitySort.OverallDesc };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);
        var items = result.Items.ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal(opp1, items[0].Id);
        Assert.Equal(opp3, items[1].Id);
        Assert.Equal(opp2, items[2].Id);
    }

    [Fact]
    public async Task SearchAsync_EvaluatedAtDescSort()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var opp3 = await CreateOpportunity("Three");

        var repo = new EfOpportunityQueryRepository(_context);

        var snapshotRecord1 = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opp1,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = "fp1",
            EvaluatedAtUtc = DateTime.UtcNow.AddHours(-2),
            Evaluated = true,
            Recommendation = "Apply",
            OverallScore = 75m,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };
        var snapshotRecord2 = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opp2,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = "fp2",
            EvaluatedAtUtc = DateTime.UtcNow.AddHours(-1),
            Evaluated = true,
            Recommendation = "Apply",
            OverallScore = 75m,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };
        var snapshotRecord3 = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opp3,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = "fp3",
            EvaluatedAtUtc = DateTime.UtcNow,
            Evaluated = true,
            Recommendation = "Apply",
            OverallScore = 75m,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };

        _context.Set<OpportunityEvaluationSnapshotRecord>().AddRange(snapshotRecord1, snapshotRecord2, snapshotRecord3);
        await _context.SaveChangesAsync();

        var criteria = new OpportunityQueryCriteria { Sort = OpportunitySort.EvaluatedAtDesc };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);
        var items = result.Items.ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal(opp3, items[0].Id);
        Assert.Equal(opp2, items[1].Id);
        Assert.Equal(opp1, items[2].Id);
    }

    [Fact]
    public async Task SearchAsync_EvaluatedAtAscSort()
    {
        var opp1 = await CreateOpportunity("One");
        var opp2 = await CreateOpportunity("Two");
        var opp3 = await CreateOpportunity("Three");

        var repo = new EfOpportunityQueryRepository(_context);

        var snapshotRecord1 = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opp1,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = "fp1",
            EvaluatedAtUtc = DateTime.UtcNow.AddHours(-2),
            Evaluated = true,
            Recommendation = "Apply",
            OverallScore = 75m,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };
        var snapshotRecord2 = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opp2,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = "fp2",
            EvaluatedAtUtc = DateTime.UtcNow.AddHours(-1),
            Evaluated = true,
            Recommendation = "Apply",
            OverallScore = 75m,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };
        var snapshotRecord3 = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opp3,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = "fp3",
            EvaluatedAtUtc = DateTime.UtcNow,
            Evaluated = true,
            Recommendation = "Apply",
            OverallScore = 75m,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };

        _context.Set<OpportunityEvaluationSnapshotRecord>().AddRange(snapshotRecord1, snapshotRecord2, snapshotRecord3);
        await _context.SaveChangesAsync();

        var criteria = new OpportunityQueryCriteria { Sort = OpportunitySort.EvaluatedAtAsc };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);
        var items = result.Items.ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal(opp1, items[0].Id);
        Assert.Equal(opp2, items[1].Id);
        Assert.Equal(opp3, items[2].Id);
    }

    [Fact]
    public async Task SearchAsync_UnevaluatedOpportunitiesSortToEnd()
    {
        var opp1 = await CreateOpportunity("Evaluated");
        var opp2 = await CreateOpportunity("Not Evaluated");

        var repo = new EfOpportunityQueryRepository(_context);

        var snapshotRecord = new OpportunityEvaluationSnapshotRecord
        {
            OpportunityId = opp1,
            ProfileId = Guid.NewGuid(),
            ProfileFingerprint = "fp1",
            EvaluatedAtUtc = DateTime.UtcNow,
            Evaluated = true,
            Recommendation = "Apply",
            OverallScore = 75m,
            PayloadSchemaVersion = 1,
            PayloadJson = CreatePayloadJson()
        };

        _context.Set<OpportunityEvaluationSnapshotRecord>().Add(snapshotRecord);
        await _context.SaveChangesAsync();

        var criteria = new OpportunityQueryCriteria { Sort = OpportunitySort.OverallAsc };

        var result = await repo.SearchAsync(criteria, CancellationToken.None);
        var items = result.Items.ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal(opp2, items[0].Id);
        Assert.Equal(opp1, items[1].Id);
    }

    [Fact]
    public async Task SearchAsync_InboxItemContainsEvaluationSummary()
    {
        var opp1 = await CreateOpportunity("One");
        await CreateSnapshot(opp1, evaluated: true, recommendation: "Prioritize", overallScore: 90m, fingerprint: "fp1");

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria();

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        var item = result.Items.Single(i => i.Id == opp1);
        Assert.NotNull(item.LatestEvaluation);
        Assert.True(item.LatestEvaluation!.Evaluated);
        Assert.Equal("Prioritize", item.LatestEvaluation.Recommendation);
        Assert.Equal(90m, item.LatestEvaluation.OverallScore);
    }

    [Fact]
    public async Task SearchAsync_InboxItemNoSnapshot_HasNullEvaluation()
    {
        var opp1 = await CreateOpportunity("One");

        var repo = new EfOpportunityQueryRepository(_context);
        var criteria = new OpportunityQueryCriteria();

        var result = await repo.SearchAsync(criteria, CancellationToken.None);

        var item = result.Items.Single(i => i.Id == opp1);
        Assert.Null(item.LatestEvaluation);
    }
}

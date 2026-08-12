using System.Text.Json;
using System.Text.Json.Serialization;
using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Persistence.Repositories;

public sealed class EfOpportunityEvaluationSnapshotRepository : IOpportunityEvaluationSnapshotRepository
{
    private readonly FreelaHunterDbContext _context;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public EfOpportunityEvaluationSnapshotRepository(FreelaHunterDbContext context)
    {
        _context = context;
    }

    public async Task SaveLatestAsync(
        OpportunityEvaluationSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var existing = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .FirstOrDefaultAsync(x => x.OpportunityId == snapshot.OpportunityId, cancellationToken);

        var payload = SnapshotPayload.FromSnapshot(snapshot);
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        if (existing is not null)
        {
            existing.ProfileId = snapshot.ProfileId;
            existing.ProfileFingerprint = snapshot.ProfileFingerprint;
            existing.EvaluatedAtUtc = snapshot.EvaluatedAt.UtcDateTime;
            existing.Evaluated = snapshot.Evaluated;
            existing.Recommendation = snapshot.Recommendation.ToString();
            existing.OverallScore = snapshot.OverallScore;
            existing.PayloadSchemaVersion = 1;
            existing.PayloadJson = payloadJson;
        }
        else
        {
            var record = new OpportunityEvaluationSnapshotRecord
            {
                OpportunityId = snapshot.OpportunityId,
                ProfileId = snapshot.ProfileId,
                ProfileFingerprint = snapshot.ProfileFingerprint,
                EvaluatedAtUtc = snapshot.EvaluatedAt.UtcDateTime,
                Evaluated = snapshot.Evaluated,
                Recommendation = snapshot.Recommendation.ToString(),
                OverallScore = snapshot.OverallScore,
                PayloadSchemaVersion = 1,
                PayloadJson = payloadJson
            };

            _context.Set<OpportunityEvaluationSnapshotRecord>().Add(record);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OpportunityEvaluationSnapshot?> GetLatestAsync(
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        var record = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OpportunityId == opportunityId, cancellationToken);

        return record is null ? null : Deserialize(record);
    }

    public async Task<IReadOnlyCollection<OpportunityEvaluationSnapshot>> GetLatestBatchAsync(
        IReadOnlyCollection<Guid> opportunityIds,
        CancellationToken cancellationToken)
    {
        if (opportunityIds.Count == 0)
            return [];

        var records = await _context.Set<OpportunityEvaluationSnapshotRecord>()
            .AsNoTracking()
            .Where(x => opportunityIds.Contains(x.OpportunityId))
            .ToListAsync(cancellationToken);

        return records.Select(Deserialize).ToList().AsReadOnly();
    }

    private static OpportunityEvaluationSnapshot Deserialize(OpportunityEvaluationSnapshotRecord record)
    {
        var payload = JsonSerializer.Deserialize<SnapshotPayload>(record.PayloadJson, JsonOptions)!;

        var analysis = new OpportunitySemanticAnalysis
        {
            EstimatedHours = payload.EstimatedHours,
            DeliveryRisk = Enum.Parse<DeliveryRiskLevel>(payload.DeliveryRisk),
            AiLeverage = Enum.Parse<AiLeverageLevel>(payload.AiLeverage),
            Confidence = Enum.Parse<AnalysisConfidence>(payload.Confidence),
            TechnicalRequirements = payload.TechnicalRequirements,
            RiskFactors = payload.RiskFactors,
            AiLeverageReasons = payload.AiLeverageReasons,
            ClarifyingQuestions = payload.ClarifyingQuestions,
            Usage = payload.UsageProvider is not null
                ? new LlmUsage
                {
                    InputTokens = payload.UsageInputTokens,
                    OutputTokens = payload.UsageOutputTokens,
                    TotalTokens = payload.UsageTotalTokens,
                    Provider = payload.UsageProvider,
                    Model = payload.UsageModel
                }
                : null
        };

        var scoreComponents = new Dictionary<string, ScoreComponentInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["technicalFit"] = payload.TechnicalFit,
            ["economicPotential"] = payload.EconomicPotential,
            ["clientQuality"] = payload.ClientQuality,
            ["competition"] = payload.Competition,
            ["deliveryConfidence"] = payload.DeliveryConfidence,
            ["aiLeverage"] = payload.AiLeverageScore
        };

        var score = OpportunityScore.Create(
            ScoreValue.From(payload.OverallScore),
            CreateScoreComponent(scoreComponents["technicalFit"]),
            CreateScoreComponent(scoreComponents["economicPotential"]),
            CreateScoreComponent(scoreComponents["clientQuality"]),
            CreateScoreComponent(scoreComponents["competition"]),
            CreateScoreComponent(scoreComponents["deliveryConfidence"]),
            CreateScoreComponent(scoreComponents["aiLeverage"]),
            []);

        var scoringInputs = new ScoringInputSummary
        {
            ProfileSkills = payload.ProfileSkills?
                .Select(s => new SkillCapabilitySummary { Name = s.Name, Level = s.Level })
                .ToList()
                .AsReadOnly() ?? [],
            TargetHourlyRate = new HourlyRateSummary
            {
                Amount = payload.TargetHourlyRateAmount,
                Currency = payload.TargetHourlyRateCurrency
            },
            EstimatedHours = payload.EstimatedHours,
            DeliveryRisk = Enum.Parse<DeliveryRiskLevel>(payload.DeliveryRisk),
            AiLeverage = Enum.Parse<AiLeverageLevel>(payload.AiLeverage),
            TechnicalFitSource = payload.TechnicalFitSource ?? "Opportunity.RequiredSkills + FreelancerProfile.Skills",
            EstimatedHoursSource = payload.EstimatedHoursSource ?? "OpportunitySemanticAnalysis",
            DeliveryRiskSource = payload.DeliveryRiskSource ?? "OpportunitySemanticAnalysis",
            AiLeverageSource = payload.AiLeverageSource ?? "OpportunitySemanticAnalysis",
            TargetHourlyRateSource = payload.TargetHourlyRateSource ?? "FreelancerProfile"
        };

        return new OpportunityEvaluationSnapshot
        {
            OpportunityId = record.OpportunityId,
            ProfileId = record.ProfileId,
            ProfileFingerprint = record.ProfileFingerprint,
            EvaluatedAt = new DateTimeOffset(record.EvaluatedAtUtc, TimeSpan.Zero),
            Evaluated = record.Evaluated,
            Recommendation = Enum.Parse<OpportunityRecommendation>(record.Recommendation),
            OverallScore = record.OverallScore,
            Analysis = analysis,
            Score = score,
            ScoringInputs = scoringInputs,
            IsStale = false
        };
    }

    private static ScoreComponent CreateScoreComponent(ScoreComponentInfo info)
    {
        return ScoreComponent.Create(
            ScoreValue.From(info.Score),
            info.Weight,
            info.Rules?.Select(r => Enum.Parse<ScoringRuleCode>(r)).ToList().AsReadOnly() ?? []);
    }

    public sealed class SnapshotPayload
    {
        public decimal EstimatedHours { get; set; }
        public string DeliveryRisk { get; set; } = string.Empty;
        public string AiLeverage { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public List<string> TechnicalRequirements { get; set; } = [];
        public List<string> RiskFactors { get; set; } = [];
        public List<string> AiLeverageReasons { get; set; } = [];
        public List<string> ClarifyingQuestions { get; set; } = [];
        public string? UsageProvider { get; set; }
        public string? UsageModel { get; set; }
        public int? UsageInputTokens { get; set; }
        public int? UsageOutputTokens { get; set; }
        public int? UsageTotalTokens { get; set; }
        public decimal OverallScore { get; set; }
        public ScoreComponentInfo TechnicalFit { get; set; } = new();
        public ScoreComponentInfo EconomicPotential { get; set; } = new();
        public ScoreComponentInfo ClientQuality { get; set; } = new();
        public ScoreComponentInfo Competition { get; set; } = new();
        public ScoreComponentInfo DeliveryConfidence { get; set; } = new();
        public ScoreComponentInfo AiLeverageScore { get; set; } = new();
        public List<SkillCapabilityInfo>? ProfileSkills { get; set; }
        public decimal TargetHourlyRateAmount { get; set; }
        public string TargetHourlyRateCurrency { get; set; } = string.Empty;
        public string? TechnicalFitSource { get; set; }
        public string? EstimatedHoursSource { get; set; }
        public string? DeliveryRiskSource { get; set; }
        public string? AiLeverageSource { get; set; }
        public string? TargetHourlyRateSource { get; set; }

        public static SnapshotPayload FromSnapshot(OpportunityEvaluationSnapshot snapshot)
        {
            return new SnapshotPayload
            {
                EstimatedHours = snapshot.Analysis.EstimatedHours,
                DeliveryRisk = snapshot.Analysis.DeliveryRisk.ToString(),
                AiLeverage = snapshot.Analysis.AiLeverage.ToString(),
                Confidence = snapshot.Analysis.Confidence.ToString(),
                TechnicalRequirements = snapshot.Analysis.TechnicalRequirements.ToList(),
                RiskFactors = snapshot.Analysis.RiskFactors.ToList(),
                AiLeverageReasons = snapshot.Analysis.AiLeverageReasons.ToList(),
                ClarifyingQuestions = snapshot.Analysis.ClarifyingQuestions.ToList(),
                UsageProvider = snapshot.Analysis.Usage?.Provider,
                UsageModel = snapshot.Analysis.Usage?.Model,
                UsageInputTokens = snapshot.Analysis.Usage?.InputTokens,
                UsageOutputTokens = snapshot.Analysis.Usage?.OutputTokens,
                UsageTotalTokens = snapshot.Analysis.Usage?.TotalTokens,
                OverallScore = snapshot.Score.Overall.Value,
                TechnicalFit = ToInfo(snapshot.Score.TechnicalFit),
                EconomicPotential = ToInfo(snapshot.Score.EconomicPotential),
                ClientQuality = ToInfo(snapshot.Score.ClientQuality),
                Competition = ToInfo(snapshot.Score.Competition),
                DeliveryConfidence = ToInfo(snapshot.Score.DeliveryConfidence),
                AiLeverageScore = ToInfo(snapshot.Score.AiLeverage),
                ProfileSkills = snapshot.ScoringInputs.ProfileSkills?
                    .Select(s => new SkillCapabilityInfo { Name = s.Name, Level = s.Level })
                    .ToList(),
                TargetHourlyRateAmount = snapshot.ScoringInputs.TargetHourlyRate.Amount,
                TargetHourlyRateCurrency = snapshot.ScoringInputs.TargetHourlyRate.Currency,
                TechnicalFitSource = snapshot.ScoringInputs.TechnicalFitSource,
                EstimatedHoursSource = snapshot.ScoringInputs.EstimatedHoursSource,
                DeliveryRiskSource = snapshot.ScoringInputs.DeliveryRiskSource,
                AiLeverageSource = snapshot.ScoringInputs.AiLeverageSource,
                TargetHourlyRateSource = snapshot.ScoringInputs.TargetHourlyRateSource
            };
        }

        private static ScoreComponentInfo ToInfo(ScoreComponent component)
        {
            return new ScoreComponentInfo
            {
                Score = component.Score.Value,
                Weight = component.Weight,
                WeightedContribution = component.WeightedContribution,
                Rules = component.Rules.Select(r => r.ToString()).ToList()
            };
        }
    }

    public sealed class ScoreComponentInfo
    {
        public decimal Score { get; set; }
        public decimal Weight { get; set; }
        public decimal WeightedContribution { get; set; }
        public List<string> Rules { get; set; } = [];
    }

    public sealed class SkillCapabilityInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }
}

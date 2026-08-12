using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application;

public sealed record OpportunityEvaluationSnapshot
{
    public required Guid OpportunityId { get; init; }
    public required Guid ProfileId { get; init; }
    public required string ProfileFingerprint { get; init; }
    public required DateTimeOffset EvaluatedAt { get; init; }
    public required bool Evaluated { get; init; }
    public required OpportunityRecommendation Recommendation { get; init; }
    public required decimal OverallScore { get; init; }
    public required OpportunitySemanticAnalysis Analysis { get; init; }
    public required Domain.ValueObjects.OpportunityScore Score { get; init; }
    public required ScoringInputSummary ScoringInputs { get; init; }
    public required bool IsStale { get; init; }
}

namespace FreelaHunter.Infrastructure.Persistence.Models;

public sealed class OpportunityEvaluationSnapshotRecord
{
    public Guid OpportunityId { get; set; }
    public Guid ProfileId { get; set; }
    public string ProfileFingerprint { get; set; } = string.Empty;
    public DateTime EvaluatedAtUtc { get; set; }
    public bool Evaluated { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public decimal OverallScore { get; set; }
    public int PayloadSchemaVersion { get; set; }
    public string PayloadJson { get; set; } = string.Empty;

    public OpportunityRecord Opportunity { get; set; } = null!;
}

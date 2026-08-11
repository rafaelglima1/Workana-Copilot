namespace FreelaHunter.Contracts.Opportunities;

public sealed class OpportunityEvaluationResponse
{
    public string OpportunityId { get; set; } = string.Empty;
    public OpportunityAnalysisResponse Analysis { get; set; } = new();
    public ScoringInputSummaryResponse ScoringInputs { get; set; } = new();
    public OpportunityScoreResponse Score { get; set; } = new();
}

public sealed class ScoringInputSummaryResponse
{
    public List<SkillCapabilitySummaryResponse> ProfileSkills { get; set; } = [];
    public HourlyRateSummaryResponse TargetHourlyRate { get; set; } = new();
    public decimal EstimatedHours { get; set; }
    public string DeliveryRisk { get; set; } = string.Empty;
    public string AiLeverage { get; set; } = string.Empty;
    public string TechnicalFitSource { get; set; } = string.Empty;
    public string EstimatedHoursSource { get; set; } = string.Empty;
    public string DeliveryRiskSource { get; set; } = string.Empty;
    public string AiLeverageSource { get; set; } = string.Empty;
    public string TargetHourlyRateSource { get; set; } = string.Empty;
}

public sealed class SkillCapabilitySummaryResponse
{
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
}

public sealed class HourlyRateSummaryResponse
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

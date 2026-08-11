using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public sealed record OpportunityEvaluation
{
    public required OpportunityId OpportunityId { get; init; }
    public required OpportunitySemanticAnalysis Analysis { get; init; }
    public required OpportunityScore Score { get; init; }
    public required ScoringInputSummary ScoringInputs { get; init; }
}

public sealed record ScoringInputSummary
{
    public IReadOnlyCollection<SkillCapabilitySummary> ProfileSkills { get; init; } = [];
    public HourlyRateSummary TargetHourlyRate { get; init; } = new();
    public decimal EstimatedHours { get; init; }
    public DeliveryRiskLevel DeliveryRisk { get; init; }
    public AiLeverageLevel AiLeverage { get; init; }
    public string TechnicalFitSource { get; init; } = "Opportunity.RequiredSkills + FreelancerProfile.Skills";
    public string EstimatedHoursSource { get; init; } = "OpportunitySemanticAnalysis";
    public string DeliveryRiskSource { get; init; } = "OpportunitySemanticAnalysis";
    public string AiLeverageSource { get; init; } = "OpportunitySemanticAnalysis";
    public string TargetHourlyRateSource { get; init; } = "FreelancerProfile";
}

public sealed record SkillCapabilitySummary
{
    public string Name { get; init; } = string.Empty;
    public string Level { get; init; } = string.Empty;
}

public sealed record HourlyRateSummary
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
}

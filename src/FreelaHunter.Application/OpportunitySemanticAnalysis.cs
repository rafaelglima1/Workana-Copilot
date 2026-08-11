using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application;

public sealed record OpportunitySemanticAnalysis
{
    public decimal EstimatedHours { get; init; }
    public DeliveryRiskLevel DeliveryRisk { get; init; }
    public AiLeverageLevel AiLeverage { get; init; }
    public AnalysisConfidence Confidence { get; init; }
    public IReadOnlyCollection<string> TechnicalRequirements { get; init; } = [];
    public IReadOnlyCollection<string> RiskFactors { get; init; } = [];
    public IReadOnlyCollection<string> AiLeverageReasons { get; init; } = [];
    public IReadOnlyCollection<string> ClarifyingQuestions { get; init; } = [];
    public LlmUsage? Usage { get; init; }
}

public sealed record LlmUsage
{
    public int? InputTokens { get; init; }
    public int? OutputTokens { get; init; }
    public int? TotalTokens { get; init; }
    public string? Provider { get; init; }
    public string? Model { get; init; }
}

public enum AnalysisConfidence
{
    Low = 0,
    Medium = 1,
    High = 2
}

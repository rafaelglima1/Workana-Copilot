namespace FreelaHunter.Contracts.Opportunities;

public sealed class OpportunityAnalysisResponse
{
    public decimal EstimatedHours { get; set; }
    public string DeliveryRisk { get; set; } = string.Empty;
    public string AiLeverage { get; set; } = string.Empty;
    public string Confidence { get; set; } = string.Empty;
    public List<string> TechnicalRequirements { get; set; } = [];
    public List<string> RiskFactors { get; set; } = [];
    public List<string> AiLeverageReasons { get; set; } = [];
    public List<string> ClarifyingQuestions { get; set; } = [];
    public LlmUsageResponse? Usage { get; set; }
}

public sealed class LlmUsageResponse
{
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
    public string? Provider { get; set; }
    public string? Model { get; set; }
}

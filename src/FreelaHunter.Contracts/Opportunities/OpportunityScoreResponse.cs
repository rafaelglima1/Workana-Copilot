namespace FreelaHunter.Contracts.Opportunities;

public sealed class OpportunityScoreResponse
{
    public decimal Overall { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public ScoreComponentResponse TechnicalFit { get; set; } = new();
    public ScoreComponentResponse EconomicPotential { get; set; } = new();
    public ScoreComponentResponse ClientQuality { get; set; } = new();
    public ScoreComponentResponse Competition { get; set; } = new();
    public ScoreComponentResponse DeliveryConfidence { get; set; } = new();
    public ScoreComponentResponse AiLeverage { get; set; } = new();
}

public sealed class ScoreComponentResponse
{
    public decimal Score { get; set; }
    public decimal Weight { get; set; }
    public decimal WeightedContribution { get; set; }
    public List<string> Rules { get; set; } = [];
}

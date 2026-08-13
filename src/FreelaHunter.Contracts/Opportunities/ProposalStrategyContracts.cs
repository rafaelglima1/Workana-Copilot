namespace FreelaHunter.Contracts.Opportunities;

public enum ContractProposalDecision
{
    Go = 0,
    Maybe = 1,
    NoGo = 2
}

public sealed class ProposalStrategyResponse
{
    public string Decision { get; set; } = string.Empty;
    public decimal RecommendedHourlyRate { get; set; }
    public string RecommendedCurrency { get; set; } = string.Empty;
    public int EstimatedDeliveryDays { get; set; }
    public decimal EstimatedRevenue { get; set; }
    public decimal Confidence { get; set; }
    public List<string> Rationale { get; set; } = [];
}

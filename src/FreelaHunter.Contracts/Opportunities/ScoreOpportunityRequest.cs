namespace FreelaHunter.Contracts.Opportunities;

public sealed class ScoreOpportunityRequest
{
    public List<SkillInput>? Skills { get; set; }
    public required RateTargetInput TargetHourlyRate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public ContractDeliveryRisk DeliveryRisk { get; set; }
    public ContractAiLeverage AiLeverage { get; set; }
}

public sealed class SkillInput
{
    public required string Name { get; set; }
    public ContractSkillLevel Level { get; set; }
}

public sealed class RateTargetInput
{
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
}

public enum ContractSkillLevel
{
    None = 0,
    Basic = 1,
    Working = 2,
    Strong = 3,
    Expert = 4
}

public enum ContractDeliveryRisk
{
    Low = 0,
    Moderate = 1,
    High = 2,
    Critical = 3
}

public enum ContractAiLeverage
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}

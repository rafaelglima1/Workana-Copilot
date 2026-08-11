using FreelaHunter.Contracts.Opportunities;

namespace FreelaHunter.Contracts.FreelancerProfile;

public sealed class SaveFreelancerProfileRequest
{
    public List<ProfileSkillInput> Skills { get; set; } = [];
    public List<ProfileRateInput> HourlyRateTargets { get; set; } = [];
    public required string DefaultRateCurrency { get; set; }
}

public sealed class ProfileSkillInput
{
    public required string Name { get; set; }
    public ContractSkillLevel Level { get; set; }
}

public sealed class ProfileRateInput
{
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
}

public sealed class FreelancerProfileResponse
{
    public Guid Id { get; set; }
    public List<ProfileSkillResponse> Skills { get; set; } = [];
    public List<ProfileRateResponse> HourlyRateTargets { get; set; } = [];
    public string DefaultRateCurrency { get; set; } = string.Empty;
}

public sealed class ProfileSkillResponse
{
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
}

public sealed class ProfileRateResponse
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public sealed class ScoreOpportunityWithProfileRequest
{
    public decimal? EstimatedHours { get; set; }
    public ContractDeliveryRisk DeliveryRisk { get; set; }
    public ContractAiLeverage AiLeverage { get; set; }
}

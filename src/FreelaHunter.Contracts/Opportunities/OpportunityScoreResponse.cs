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
    public List<SkillMatchEvidenceResponse> TechnicalSkillMatches { get; set; } = [];
}

public sealed class ScoreComponentResponse
{
    public decimal Score { get; set; }
    public decimal Weight { get; set; }
    public decimal WeightedContribution { get; set; }
    public List<string> Rules { get; set; } = [];
}

public sealed class SkillMatchEvidenceResponse
{
    public string RequiredSkill { get; set; } = string.Empty;
    public string? MatchedProfileSkill { get; set; }
    public string? CanonicalSkill { get; set; }
    public string MatchKind { get; set; } = string.Empty;
    public string? ProfileLevel { get; set; }
    public decimal Score { get; set; }
}

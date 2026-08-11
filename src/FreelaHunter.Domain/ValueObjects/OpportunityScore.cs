using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record OpportunityScore
{
    public ScoreValue Overall { get; }
    public OpportunityRecommendation Recommendation { get; }
    public ScoreComponent TechnicalFit { get; }
    public ScoreComponent EconomicPotential { get; }
    public ScoreComponent ClientQuality { get; }
    public ScoreComponent Competition { get; }
    public ScoreComponent DeliveryConfidence { get; }
    public ScoreComponent AiLeverage { get; }
    public IReadOnlyCollection<SkillMatchEvidence> TechnicalSkillMatches { get; }

    private OpportunityScore(
        ScoreValue overall,
        OpportunityRecommendation recommendation,
        ScoreComponent technicalFit,
        ScoreComponent economicPotential,
        ScoreComponent clientQuality,
        ScoreComponent competition,
        ScoreComponent deliveryConfidence,
        ScoreComponent aiLeverage,
        IReadOnlyCollection<SkillMatchEvidence> technicalSkillMatches)
    {
        Overall = overall;
        Recommendation = recommendation;
        TechnicalFit = technicalFit;
        EconomicPotential = economicPotential;
        ClientQuality = clientQuality;
        Competition = competition;
        DeliveryConfidence = deliveryConfidence;
        AiLeverage = aiLeverage;
        TechnicalSkillMatches = technicalSkillMatches;
    }

    public static OpportunityScore Create(
        ScoreValue overall,
        ScoreComponent technicalFit,
        ScoreComponent economicPotential,
        ScoreComponent clientQuality,
        ScoreComponent competition,
        ScoreComponent deliveryConfidence,
        ScoreComponent aiLeverage,
        IReadOnlyCollection<SkillMatchEvidence>? technicalSkillMatches = null)
    {
        var recommendation = overall.Value switch
        {
            >= 80 => OpportunityRecommendation.Prioritize,
            >= 65 => OpportunityRecommendation.Apply,
            >= 50 => OpportunityRecommendation.ConsiderWithCaution,
            _ => OpportunityRecommendation.Skip
        };

        return new OpportunityScore(
            overall,
            recommendation,
            technicalFit,
            economicPotential,
            clientQuality,
            competition,
            deliveryConfidence,
            aiLeverage,
            technicalSkillMatches ?? Array.Empty<SkillMatchEvidence>());
    }
}

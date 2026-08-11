using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class ScoreOpportunityWithProfileHandler
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IFreelancerProfileRepository _profileRepository;

    public ScoreOpportunityWithProfileHandler(
        IOpportunityRepository opportunityRepository,
        IFreelancerProfileRepository profileRepository)
    {
        _opportunityRepository = opportunityRepository;
        _profileRepository = profileRepository;
    }

    public async Task<OpportunityScoreResponse?> HandleAsync(
        Guid opportunityId,
        ScoreOpportunityWithProfileRequest request,
        CancellationToken cancellationToken)
    {
        var id = OpportunityId.From(opportunityId);
        var opportunity = await _opportunityRepository.GetByIdAsync(id, cancellationToken);

        if (opportunity is null)
            return null;

        var profile = await _profileRepository.GetCurrentAsync(cancellationToken);

        if (profile is null)
            return null;

        var preferredCurrency = opportunity.Budget.Currency;
        var resolvedRate = profile.ResolveHourlyRate(preferredCurrency);

        var context = OpportunityScoringContext.Create(
            profile.Skills,
            resolvedRate,
            request.EstimatedHours,
            MapDeliveryRisk(request.DeliveryRisk),
            MapAiLeverage(request.AiLeverage));

        var score = OpportunityScorer.Score(opportunity, context);

        return MapToResponse(score);
    }

    private static DeliveryRiskLevel MapDeliveryRisk(ContractDeliveryRisk contract) => contract switch
    {
        ContractDeliveryRisk.Low => DeliveryRiskLevel.Low,
        ContractDeliveryRisk.Moderate => DeliveryRiskLevel.Moderate,
        ContractDeliveryRisk.High => DeliveryRiskLevel.High,
        ContractDeliveryRisk.Critical => DeliveryRiskLevel.Critical,
        _ => DeliveryRiskLevel.Moderate
    };

    private static AiLeverageLevel MapAiLeverage(ContractAiLeverage contract) => contract switch
    {
        ContractAiLeverage.None => AiLeverageLevel.None,
        ContractAiLeverage.Low => AiLeverageLevel.Low,
        ContractAiLeverage.Medium => AiLeverageLevel.Medium,
        ContractAiLeverage.High => AiLeverageLevel.High,
        ContractAiLeverage.VeryHigh => AiLeverageLevel.VeryHigh,
        _ => AiLeverageLevel.None
    };

    private static OpportunityScoreResponse MapToResponse(OpportunityScore score)
    {
        return new OpportunityScoreResponse
        {
            Overall = score.Overall.Value,
            Recommendation = score.Recommendation.ToString(),
            TechnicalFit = MapComponent(score.TechnicalFit),
            EconomicPotential = MapComponent(score.EconomicPotential),
            ClientQuality = MapComponent(score.ClientQuality),
            Competition = MapComponent(score.Competition),
            DeliveryConfidence = MapComponent(score.DeliveryConfidence),
            AiLeverage = MapComponent(score.AiLeverage)
        };
    }

    private static ScoreComponentResponse MapComponent(ScoreComponent component)
    {
        return new ScoreComponentResponse
        {
            Score = component.Score.Value,
            Weight = component.Weight,
            WeightedContribution = component.WeightedContribution,
            Rules = component.Rules.Select(r => r.ToString()).ToList()
        };
    }
}

using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class ScoreOpportunity
{
    private readonly IOpportunityRepository _repository;

    public ScoreOpportunity(IOpportunityRepository repository)
    {
        _repository = repository;
    }

    public async Task<OpportunityScoreResponse?> HandleAsync(
        Guid opportunityId,
        ScoreOpportunityRequest request,
        CancellationToken cancellationToken)
    {
        var id = OpportunityId.From(opportunityId);
        var opportunity = await _repository.GetByIdAsync(id, cancellationToken);

        if (opportunity is null)
            return null;

        var context = MapContext(request);
        var score = OpportunityScorer.Score(opportunity, context);

        return MapToResponse(score);
    }

    private static OpportunityScoringContext MapContext(ScoreOpportunityRequest request)
    {
        var skills = request.Skills?
            .Select(s => SkillCapability.Create(s.Name, MapSkillLevel(s.Level)))
            ?? [];

        var targetRate = HourlyRateTarget.Create(
            request.TargetHourlyRate.Amount,
            CurrencyCode.From(request.TargetHourlyRate.Currency));

        return OpportunityScoringContext.Create(
            skills,
            targetRate,
            request.EstimatedHours,
            MapDeliveryRisk(request.DeliveryRisk),
            MapAiLeverage(request.AiLeverage));
    }

    private static SkillLevel MapSkillLevel(ContractSkillLevel contract) => contract switch
    {
        ContractSkillLevel.None => SkillLevel.None,
        ContractSkillLevel.Basic => SkillLevel.Basic,
        ContractSkillLevel.Working => SkillLevel.Working,
        ContractSkillLevel.Strong => SkillLevel.Strong,
        ContractSkillLevel.Expert => SkillLevel.Expert,
        _ => SkillLevel.None
    };

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

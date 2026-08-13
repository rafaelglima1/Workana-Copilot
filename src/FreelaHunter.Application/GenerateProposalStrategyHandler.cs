using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class GenerateProposalStrategyHandler
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IFreelancerProfileRepository _profileRepository;
    private readonly IOpportunityEvaluationSnapshotRepository _snapshotRepository;

    public GenerateProposalStrategyHandler(
        IOpportunityRepository opportunityRepository,
        IFreelancerProfileRepository profileRepository,
        IOpportunityEvaluationSnapshotRepository snapshotRepository)
    {
        _opportunityRepository = opportunityRepository;
        _profileRepository = profileRepository;
        _snapshotRepository = snapshotRepository;
    }

    public async Task<ProposalStrategyResponse?> HandleAsync(
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        var id = OpportunityId.From(opportunityId);
        var opportunity = await _opportunityRepository.GetByIdAsync(id, cancellationToken);

        if (opportunity is null)
            return null;

        var profile = await _profileRepository.GetCurrentAsync(cancellationToken);

        if (profile is null)
            return null;

        var snapshot = await _snapshotRepository.GetLatestAsync(opportunityId, cancellationToken);

        if (snapshot is null || snapshot.IsStale)
            return null;

        var strategy = ProposalStrategist.Calculate(
            snapshot.Score,
            snapshot.Analysis.EstimatedHours,
            snapshot.Analysis.DeliveryRisk,
            profile,
            opportunity.Budget);

        return MapToResponse(strategy);
    }

    private static ProposalStrategyResponse MapToResponse(ProposalStrategy strategy)
    {
        return new ProposalStrategyResponse
        {
            Decision = strategy.Decision.ToString(),
            RecommendedHourlyRate = strategy.RecommendedHourlyRate,
            RecommendedCurrency = strategy.RecommendedCurrency.Value,
            EstimatedDeliveryDays = strategy.EstimatedDeliveryDays,
            EstimatedRevenue = strategy.EstimatedRevenue,
            Confidence = strategy.Confidence.Value,
            Rationale = strategy.Rationale.ToList()
        };
    }
}

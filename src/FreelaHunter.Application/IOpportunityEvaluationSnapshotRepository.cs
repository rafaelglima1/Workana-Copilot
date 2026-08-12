namespace FreelaHunter.Application;

public interface IOpportunityEvaluationSnapshotRepository
{
    Task SaveLatestAsync(OpportunityEvaluationSnapshot snapshot, CancellationToken cancellationToken);
    Task<OpportunityEvaluationSnapshot?> GetLatestAsync(Guid opportunityId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OpportunityEvaluationSnapshot>> GetLatestBatchAsync(
        IReadOnlyCollection<Guid> opportunityIds,
        CancellationToken cancellationToken);
}

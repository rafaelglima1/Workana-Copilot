using FreelaHunter.Application;

namespace FreelaHunter.Application.Tests;

internal sealed class FakeOpportunityEvaluationSnapshotRepository : IOpportunityEvaluationSnapshotRepository
{
    private readonly Dictionary<Guid, OpportunityEvaluationSnapshot> _store = [];

    public List<OpportunityEvaluationSnapshot> SavedSnapshots { get; } = [];

    public Task SaveLatestAsync(OpportunityEvaluationSnapshot snapshot, CancellationToken cancellationToken)
    {
        _store[snapshot.OpportunityId] = snapshot;
        SavedSnapshots.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task<OpportunityEvaluationSnapshot?> GetLatestAsync(
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        _store.TryGetValue(opportunityId, out var snapshot);
        return Task.FromResult(snapshot);
    }

    public Task<IReadOnlyCollection<OpportunityEvaluationSnapshot>> GetLatestBatchAsync(
        IReadOnlyCollection<Guid> opportunityIds,
        CancellationToken cancellationToken)
    {
        var results = opportunityIds
            .Where(id => _store.ContainsKey(id))
            .Select(id => _store[id])
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyCollection<OpportunityEvaluationSnapshot>>(results);
    }
}

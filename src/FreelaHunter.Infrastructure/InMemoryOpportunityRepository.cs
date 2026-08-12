using System.Collections.Concurrent;
using FreelaHunter.Application;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Infrastructure;

public sealed class InMemoryOpportunityRepository : IOpportunityRepository
{
    private readonly ConcurrentDictionary<Guid, Opportunity> _store = new();

    public Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken)
    {
        _store[opportunity.Id.Value] = opportunity;
        return Task.CompletedTask;
    }

    public Task<Opportunity?> GetByIdAsync(OpportunityId id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id.Value, out var opportunity);
        return Task.FromResult(opportunity);
    }

    public Task UpdateAsync(Opportunity opportunity, CancellationToken cancellationToken)
    {
        _store[opportunity.Id.Value] = opportunity;
        return Task.CompletedTask;
    }
}

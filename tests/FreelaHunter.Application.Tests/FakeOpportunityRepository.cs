using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

internal sealed class FakeOpportunityRepository : IOpportunityRepository
{
    private readonly Dictionary<Guid, Opportunity> _store = [];

    public List<Opportunity> AddedOpportunities { get; } = [];
    public List<Opportunity> UpdatedOpportunities { get; } = [];

    public Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken)
    {
        _store[opportunity.Id.Value] = opportunity;
        AddedOpportunities.Add(opportunity);
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
        UpdatedOpportunities.Add(opportunity);
        return Task.CompletedTask;
    }

    public Task<Opportunity?> GetByExternalReferenceAsync(string externalReference, CancellationToken cancellationToken)
    {
        var opportunity = _store.Values.FirstOrDefault(o => o.ExternalReference == externalReference);
        return Task.FromResult(opportunity);
    }
}

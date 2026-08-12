using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public interface IOpportunityRepository
{
    Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken);
    Task<Opportunity?> GetByIdAsync(OpportunityId id, CancellationToken cancellationToken);
    Task UpdateAsync(Opportunity opportunity, CancellationToken cancellationToken);
}

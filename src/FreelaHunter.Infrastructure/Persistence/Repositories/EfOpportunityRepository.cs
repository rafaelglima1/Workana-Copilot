using FreelaHunter.Application;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Persistence.Repositories;

public sealed class EfOpportunityRepository : IOpportunityRepository
{
    private readonly FreelaHunterDbContext _context;

    public EfOpportunityRepository(FreelaHunterDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Opportunity opportunity, CancellationToken cancellationToken)
    {
        var record = OpportunityPersistenceMapper.ToRecord(opportunity);
        await _context.Opportunities.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Opportunity?> GetByIdAsync(OpportunityId id, CancellationToken cancellationToken)
    {
        var record = await _context.Opportunities
            .AsNoTracking()
            .Include(o => o.Skills)
            .FirstOrDefaultAsync(o => o.Id == id.Value, cancellationToken);

        if (record is null)
            return null;

        return OpportunityPersistenceMapper.ToDomain(record);
    }

    public async Task UpdateAsync(Opportunity opportunity, CancellationToken cancellationToken)
    {
        var record = await _context.Opportunities
            .Include(o => o.Skills)
            .FirstOrDefaultAsync(o => o.Id == opportunity.Id.Value, cancellationToken);

        if (record is null)
            return;

        OpportunityPersistenceMapper.UpdateRecord(record, opportunity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

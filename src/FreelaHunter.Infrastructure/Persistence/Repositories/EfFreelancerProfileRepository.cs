using FreelaHunter.Application;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Persistence.Repositories;

public sealed class EfFreelancerProfileRepository : IFreelancerProfileRepository
{
    private readonly FreelaHunterDbContext _context;

    public EfFreelancerProfileRepository(FreelaHunterDbContext context)
    {
        _context = context;
    }

    public async Task<FreelancerProfile?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        var record = await _context.FreelancerProfiles
            .AsNoTracking()
            .Include(p => p.Skills)
            .Include(p => p.HourlyRates)
            .FirstOrDefaultAsync(cancellationToken);

        if (record is null)
            return null;

        return FreelancerProfilePersistenceMapper.ToDomain(record);
    }

    public async Task SaveAsync(FreelancerProfile profile, CancellationToken cancellationToken)
    {
        var existing = await _context.FreelancerProfiles
            .Include(p => p.Skills)
            .Include(p => p.HourlyRates)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            _context.FreelancerProfileSkills.RemoveRange(existing.Skills);
            _context.FreelancerHourlyRates.RemoveRange(existing.HourlyRates);
            _context.FreelancerProfiles.Remove(existing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var record = FreelancerProfilePersistenceMapper.ToRecord(profile);
        await _context.FreelancerProfiles.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

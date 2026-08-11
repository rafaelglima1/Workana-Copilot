using FreelaHunter.Domain.Entities;

namespace FreelaHunter.Application;

public interface IFreelancerProfileRepository
{
    Task<FreelancerProfile?> GetCurrentAsync(CancellationToken cancellationToken);
    Task SaveAsync(FreelancerProfile profile, CancellationToken cancellationToken);
}

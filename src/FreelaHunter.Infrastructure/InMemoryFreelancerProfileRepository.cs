using FreelaHunter.Application;
using FreelaHunter.Domain.Entities;

namespace FreelaHunter.Infrastructure;

public sealed class InMemoryFreelancerProfileRepository : IFreelancerProfileRepository
{
    private FreelancerProfile? _current;
    private readonly object _lock = new();

    public Task<FreelancerProfile?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return Task.FromResult(_current);
        }
    }

    public Task SaveAsync(FreelancerProfile profile, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _current = profile;
        }
        return Task.CompletedTask;
    }
}

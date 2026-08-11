namespace FreelaHunter.Application;

public interface IHealthCheckService
{
    Task<string> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

public class HealthCheckService : IHealthCheckService
{
    public Task<string> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult("Healthy");
    }
}

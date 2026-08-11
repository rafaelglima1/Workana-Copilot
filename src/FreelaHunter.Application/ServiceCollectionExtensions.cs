using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        return services;
    }
}

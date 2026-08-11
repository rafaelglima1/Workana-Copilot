using FreelaHunter.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IOpportunityRepository, InMemoryOpportunityRepository>();
        return services;
    }
}

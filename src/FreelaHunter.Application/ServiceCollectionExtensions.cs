using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddSingleton<CreateManualOpportunityHandler>();
        services.AddSingleton<GetOpportunityByIdHandler>();
        services.AddSingleton<ScoreOpportunity>();
        services.AddSingleton<AnalyzeOpportunityHandler>();
        return services;
    }
}

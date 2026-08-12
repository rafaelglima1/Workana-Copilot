using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IHealthCheckService, HealthCheckService>();
        services.AddScoped<CreateManualOpportunityHandler>();
        services.AddScoped<GetOpportunityByIdHandler>();
        services.AddScoped<ScoreOpportunity>();
        services.AddScoped<AnalyzeOpportunityHandler>();
        services.AddScoped<SaveFreelancerProfileHandler>();
        services.AddScoped<GetFreelancerProfileHandler>();
        services.AddScoped<ScoreOpportunityWithProfileHandler>();
        services.AddScoped<EvaluateOpportunityHandler>();
        return services;
    }
}

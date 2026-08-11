using FreelaHunter.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IOpportunityRepository, InMemoryOpportunityRepository>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IOpportunityRepository, InMemoryOpportunityRepository>();

        services.Configure<MiMoOptions>(
            configuration.GetSection(MiMoOptions.SectionName));

        services.AddHttpClient<IOpportunityIntelligenceGateway, MiMoOpportunityIntelligenceGateway>(
            client =>
            {
                var options = new MiMoOptions();
                configuration.GetSection(MiMoOptions.SectionName).Bind(options);
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        return services;
    }
}

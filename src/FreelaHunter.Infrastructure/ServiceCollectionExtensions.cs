using FreelaHunter.Application;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<IOpportunityRepository, InMemoryOpportunityRepository>();
        services.AddSingleton<IFreelancerProfileRepository, InMemoryFreelancerProfileRepository>();
        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FreelaHunter")
            ?? "Data Source=freelahunter.db";

        services.AddDbContext<FreelaHunterDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IOpportunityRepository, EfOpportunityRepository>();
        services.AddScoped<IOpportunityQueryRepository, EfOpportunityQueryRepository>();
        services.AddScoped<IOpportunityEvaluationSnapshotRepository, EfOpportunityEvaluationSnapshotRepository>();
        services.AddScoped<IFreelancerProfileRepository, EfFreelancerProfileRepository>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        services.Configure<MiMoOptions>(
            configuration.GetSection(MiMoOptions.SectionName));

        services.Configure<WorkanaScraperOptions>(
            configuration.GetSection(WorkanaScraperOptions.SectionName));

        services.AddHttpClient<IOpportunityIntelligenceGateway, MiMoOpportunityIntelligenceGateway>(
            client =>
            {
                var options = new MiMoOptions();
                configuration.GetSection(MiMoOptions.SectionName).Bind(options);
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        services.AddSingleton<IWorkanaScraper, PlaywrightWorkanaScraper>();

        return services;
    }
}

using FreelaHunter.Application;
using FreelaHunter.Application.Providers;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;
using FreelaHunter.Infrastructure.Providers;
using FreelaHunter.Infrastructure.Providers.Nerdin;
using FreelaHunter.Infrastructure.Providers.Workana;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services, IConfiguration configuration)
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

        services.Configure<TelegramOptions>(
            configuration.GetSection(TelegramOptions.SectionName));

        services.AddSingleton<ITelegramNotifier, TelegramNotifier>();

        services.AddHttpClient<IOpportunityIntelligenceGateway, MiMoOpportunityIntelligenceGateway>(
            client =>
            {
                var options = new MiMoOptions();
                configuration.GetSection(MiMoOptions.SectionName).Bind(options);
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        // Camoufox options
        services.Configure<CamoufoxOptions>(
            configuration.GetSection(CamoufoxOptions.SectionName));

        // Workana provider (legacy IWorkanaScraper + new IJobProvider)
        services.Configure<WorkanaProviderOptions>(
            configuration.GetSection(WorkanaProviderOptions.SectionName));
#pragma warning disable CS0618 // Intentional backward compatibility with legacy IWorkanaScraper
        services.AddSingleton<IWorkanaScraper, WorkanaProvider>();
#pragma warning restore CS0618
        services.AddSingleton<IJobProvider, WorkanaProvider>();

        // Nerdin provider
        services.Configure<NerdinProviderOptions>(
            configuration.GetSection(NerdinProviderOptions.SectionName));
        services.AddSingleton<IJobProvider, NerdinProvider>();

        // Job Provider Registry
        services.AddSingleton(sp =>
        {
            var registry = new JobProviderRegistry();
            var providers = sp.GetServices<IJobProvider>();
            foreach (var provider in providers)
                registry.Register(provider);
            return registry;
        });

        return services;
    }
}

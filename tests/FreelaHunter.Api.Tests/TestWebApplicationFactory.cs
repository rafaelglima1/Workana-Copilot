using FreelaHunter.Application;
using FreelaHunter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FreelaHunter.Api.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"freelahunter_test_{Guid.NewGuid():N}.db");

    public IOpportunityIntelligenceGateway? FakeGateway { get; set; }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.ConfigureServices(services =>
            {
                // Remove the EF DbContext registered by AddInfrastructureServices
                // and re-register with our unique test SQLite file.
                var optionsDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<FreelaHunterDbContext>));
                if (optionsDescriptor is not null)
                    services.Remove(optionsDescriptor);

                var contextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(FreelaHunterDbContext));
                if (contextDescriptor is not null)
                    services.Remove(contextDescriptor);

                services.AddDbContext<FreelaHunterDbContext>(options =>
                    options.UseSqlite($"Data Source={_dbPath};Pooling=False"));

                // Replace the MiMo HTTP gateway with a fake or test stub
                var gatewayDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IOpportunityIntelligenceGateway));
                if (gatewayDescriptor is not null)
                    services.Remove(gatewayDescriptor);

                if (FakeGateway is not null)
                {
                    services.AddSingleton(FakeGateway);
                }
                else
                {
                    services.AddSingleton<IOpportunityIntelligenceGateway, FakeApiIntelligenceGateway>();
                }
            });
        });

        return base.CreateHost(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        var baseDir = Path.GetDirectoryName(_dbPath)!;
        var fileName = Path.GetFileNameWithoutExtension(_dbPath);

        foreach (var ext in new[] { ".db", "-wal", "-shm" })
        {
            var file = Path.Combine(baseDir, fileName + ext);
            TryDelete(file);
        }
    }

    private static void TryDelete(string path)
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                if (!File.Exists(path)) return;
                File.Delete(path);
                return;
            }
            catch (IOException) when (i < 2)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException) when (i < 2)
            {
                Thread.Sleep(50);
            }
        }
    }
}

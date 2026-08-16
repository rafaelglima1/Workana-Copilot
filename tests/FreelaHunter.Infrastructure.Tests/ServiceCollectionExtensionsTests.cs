using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FreelaHunter.Infrastructure;

namespace FreelaHunter.Infrastructure.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructureServices_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        services.AddInfrastructureServices(config);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}

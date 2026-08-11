using Microsoft.Extensions.DependencyInjection;
using FreelaHunter.Infrastructure;

namespace FreelaHunter.Infrastructure.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInfrastructureServices_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddInfrastructureServices();
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}

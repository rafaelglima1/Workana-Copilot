namespace FreelaHunter.Application.Tests;

public class HealthCheckServiceTests
{
    [Fact]
    public async Task GetHealthStatus_ReturnsHealthy()
    {
        var service = new HealthCheckService();
        var result = await service.GetHealthStatusAsync();
        Assert.Equal("Healthy", result);
    }
}

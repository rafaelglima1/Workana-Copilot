using System.Reflection;

namespace FreelaHunter.Architecture.Tests;

public class DependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(FreelaHunter.Domain.Entities.Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(FreelaHunter.Application.IHealthCheckService).Assembly;
    private static readonly Assembly ContractsAssembly = typeof(FreelaHunter.Contracts.Health.HealthResponse).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(FreelaHunter.Infrastructure.ServiceCollectionExtensions).Assembly;

    [Fact]
    public void Domain_Does_Not_Depend_On_Application()
    {
        var references = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Application", references);
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_Infrastructure()
    {
        var references = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Infrastructure", references);
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_Api()
    {
        var references = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Api", references);
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_Worker()
    {
        var references = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Worker", references);
    }

    [Fact]
    public void Domain_Does_Not_Depend_On_Web()
    {
        var references = DomainAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Web", references);
    }

    [Fact]
    public void Application_Does_Not_Depend_On_Infrastructure()
    {
        var references = ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Infrastructure", references);
    }

    [Fact]
    public void Application_Does_Not_Depend_On_Api()
    {
        var references = ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Api", references);
    }

    [Fact]
    public void Application_Does_Not_Depend_On_Worker()
    {
        var references = ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Worker", references);
    }

    [Fact]
    public void Application_Does_Not_Depend_On_Web()
    {
        var references = ApplicationAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Web", references);
    }

    [Fact]
    public void Contracts_Does_Not_Depend_On_Domain()
    {
        var references = ContractsAssembly.GetReferencedAssemblies().Select(a => a.Name);
        Assert.DoesNotContain("FreelaHunter.Domain", references);
    }
}

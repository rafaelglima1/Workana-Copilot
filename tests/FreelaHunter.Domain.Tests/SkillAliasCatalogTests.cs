using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class SkillAliasCatalogTests
{
    [Fact]
    public void ResolveCanonical_ExactMatch_ReturnsCanonical()
    {
        var catalog = SkillAliasCatalog.Default;

        Assert.Equal("C#", catalog.ResolveCanonical("C#"));
        Assert.Equal("React", catalog.ResolveCanonical("React"));
        Assert.Equal("PostgreSQL", catalog.ResolveCanonical("PostgreSQL"));
    }

    [Fact]
    public void ResolveCanonical_Alias_ReturnsCanonical()
    {
        var catalog = SkillAliasCatalog.Default;

        Assert.Equal("C#", catalog.ResolveCanonical("CSharp"));
        Assert.Equal("React", catalog.ResolveCanonical("React.js"));
        Assert.Equal("React", catalog.ResolveCanonical("ReactJS"));
        Assert.Equal("PostgreSQL", catalog.ResolveCanonical("Postgres"));
        Assert.Equal("Kubernetes", catalog.ResolveCanonical("K8s"));
    }

    [Fact]
    public void ResolveCanonical_CaseInsensitive()
    {
        var catalog = SkillAliasCatalog.Default;

        Assert.Equal("C#", catalog.ResolveCanonical("csharp"));
        Assert.Equal("React", catalog.ResolveCanonical("react.js"));
        Assert.Equal("PostgreSQL", catalog.ResolveCanonical("postgres"));
    }

    [Fact]
    public void ResolveCanonical_Unknown_ReturnsNull()
    {
        var catalog = SkillAliasCatalog.Default;

        Assert.Null(catalog.ResolveCanonical("Python"));
        Assert.Null(catalog.ResolveCanonical("Java"));
        Assert.Null(catalog.ResolveCanonical("Ruby"));
    }

    [Fact]
    public void AreEquivalent_SameCanonical_ReturnsTrue()
    {
        var catalog = SkillAliasCatalog.Default;

        Assert.True(catalog.AreEquivalent("C#", "CSharp"));
        Assert.True(catalog.AreEquivalent("React", "React.js"));
        Assert.True(catalog.AreEquivalent("PostgreSQL", "Postgres"));
        Assert.True(catalog.AreEquivalent("Kubernetes", "K8s"));
    }

    [Fact]
    public void AreEquivalent_DifferentCanonical_ReturnsFalse()
    {
        var catalog = SkillAliasCatalog.Default;

        Assert.False(catalog.AreEquivalent(".NET", "ASP.NET Core"));
        Assert.False(catalog.AreEquivalent("C#", ".NET"));
        Assert.False(catalog.AreEquivalent("API", "REST API"));
        Assert.False(catalog.AreEquivalent("SQL", "SQL Server"));
        Assert.False(catalog.AreEquivalent("SQL", "PostgreSQL"));
        Assert.False(catalog.AreEquivalent("Java", "JavaScript"));
        Assert.False(catalog.AreEquivalent("Angular", "AngularJS"));
        Assert.False(catalog.AreEquivalent("React", "React Native"));
        Assert.False(catalog.AreEquivalent("Flutter", "Dart"));
        Assert.False(catalog.AreEquivalent("Node.js", "JavaScript"));
        Assert.False(catalog.AreEquivalent("Entity Framework", "Entity Framework Core"));
    }

    [Fact]
    public void GetAliases_ReturnsExpectedAliases()
    {
        var catalog = SkillAliasCatalog.Default;

        var csharpAliases = catalog.GetAliases("C#");
        Assert.Contains("C#", csharpAliases);
        Assert.Contains("CSharp", csharpAliases);
        Assert.Contains("C Sharp", csharpAliases);

        var reactAliases = catalog.GetAliases("React");
        Assert.Contains("React", reactAliases);
        Assert.Contains("React.js", reactAliases);
        Assert.Contains("ReactJS", reactAliases);
    }

    [Fact]
    public void GetAliases_Unknown_ReturnsEmpty()
    {
        var catalog = SkillAliasCatalog.Default;

        var aliases = catalog.GetAliases("Python");
        Assert.Empty(aliases);
    }

    [Fact]
    public void Create_DuplicateAlias_DifferentCanonical_Throws()
    {
        var entries = new[]
        {
            new SkillAliasEntry("SkillA", ["SameAlias"]),
            new SkillAliasEntry("SkillB", ["SameAlias"])
        };

        Assert.Throws<Exceptions.DomainException>(() => SkillAliasCatalog.Create(entries));
    }

    [Fact]
    public void Create_EmptyCanonical_Throws()
    {
        var entries = new[]
        {
            new SkillAliasEntry("", ["Alias"])
        };

        Assert.Throws<Exceptions.DomainException>(() => SkillAliasCatalog.Create(entries));
    }

    [Fact]
    public void Create_EmptyAlias_Throws()
    {
        var entries = new[]
        {
            new SkillAliasEntry("Canonical", [""])
        };

        Assert.Throws<Exceptions.DomainException>(() => SkillAliasCatalog.Create(entries));
    }

    [Fact]
    public void Default_HasExpectedCount()
    {
        var catalog = SkillAliasCatalog.Default;

        Assert.NotNull(catalog.ResolveCanonical("C#"));
        Assert.NotNull(catalog.ResolveCanonical("JavaScript"));
        Assert.NotNull(catalog.ResolveCanonical("TypeScript"));
        Assert.NotNull(catalog.ResolveCanonical("React"));
        Assert.NotNull(catalog.ResolveCanonical("Node.js"));
        Assert.NotNull(catalog.ResolveCanonical("ASP.NET Core"));
        Assert.NotNull(catalog.ResolveCanonical(".NET"));
        Assert.NotNull(catalog.ResolveCanonical("Entity Framework Core"));
        Assert.NotNull(catalog.ResolveCanonical("PostgreSQL"));
        Assert.NotNull(catalog.ResolveCanonical("SQL Server"));
        Assert.NotNull(catalog.ResolveCanonical("MongoDB"));
        Assert.NotNull(catalog.ResolveCanonical("Kubernetes"));
        Assert.NotNull(catalog.ResolveCanonical("REST API"));
    }
}

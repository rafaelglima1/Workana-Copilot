using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed class SkillAliasCatalog
{
    private readonly Dictionary<string, string> _aliasToCanonical;
    private readonly Dictionary<string, IReadOnlyCollection<string>> _canonicalToAliases;

    public static SkillAliasCatalog Default { get; } = CreateDefault();

    private SkillAliasCatalog(
        Dictionary<string, string> aliasToCanonical,
        Dictionary<string, IReadOnlyCollection<string>> canonicalToAliases)
    {
        _aliasToCanonical = aliasToCanonical;
        _canonicalToAliases = canonicalToAliases;
    }

    public string? ResolveCanonical(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
            return null;

        var normalized = SkillNameNormalizer.Normalize(skillName);
        return _aliasToCanonical.TryGetValue(normalized, out var canonical) ? canonical : null;
    }

    public bool AreEquivalent(string skillA, string skillB)
    {
        var canonicalA = ResolveCanonical(skillA);
        var canonicalB = ResolveCanonical(skillB);

        if (canonicalA is null || canonicalB is null)
            return false;

        return string.Equals(canonicalA, canonicalB, StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> GetAliases(string canonicalName)
    {
        if (string.IsNullOrWhiteSpace(canonicalName))
            return Array.Empty<string>();

        var normalized = SkillNameNormalizer.Normalize(canonicalName);
        return _canonicalToAliases.TryGetValue(normalized, out var aliases)
            ? aliases
            : Array.Empty<string>();
    }

    public static SkillAliasCatalog Create(IEnumerable<SkillAliasEntry> entries)
    {
        var aliasToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var canonicalToAliasesInternal = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.CanonicalName))
                throw new DomainException("Canonical name cannot be empty.");

            var normalizedCanonical = SkillNameNormalizer.Normalize(entry.CanonicalName);

            if (!canonicalToAliasesInternal.ContainsKey(normalizedCanonical))
                canonicalToAliasesInternal[normalizedCanonical] = new List<string>();

            canonicalToAliasesInternal[normalizedCanonical].Add(entry.CanonicalName);

            foreach (var alias in entry.Aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                    throw new DomainException("Alias cannot be empty.");

                var normalizedAlias = SkillNameNormalizer.Normalize(alias);

                if (aliasToCanonical.TryGetValue(normalizedAlias, out var existingCanonical))
                {
                    if (!string.Equals(existingCanonical, normalizedCanonical, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new DomainException(
                            $"Alias '{alias}' is already mapped to '{existingCanonical}', cannot also map to '{normalizedCanonical}'.");
                    }
                }
                else
                {
                    aliasToCanonical[normalizedAlias] = normalizedCanonical;
                }

                if (!canonicalToAliasesInternal[normalizedCanonical].Contains(alias))
                    canonicalToAliasesInternal[normalizedCanonical].Add(alias);
            }
        }

        var canonicalToAliases = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in canonicalToAliasesInternal)
        {
            canonicalToAliases[kvp.Key] = kvp.Value.AsReadOnly();
        }

        return new SkillAliasCatalog(aliasToCanonical, canonicalToAliases);
    }

    private static SkillAliasCatalog CreateDefault()
    {
        return Create(new[]
        {
            new SkillAliasEntry("C#", ["C#", "CSharp", "C Sharp"]),
            new SkillAliasEntry("JavaScript", ["JavaScript", "JS"]),
            new SkillAliasEntry("TypeScript", ["TypeScript", "TS"]),
            new SkillAliasEntry("React", ["React", "React.js", "ReactJS"]),
            new SkillAliasEntry("Node.js", ["Node.js", "NodeJS"]),
            new SkillAliasEntry("ASP.NET Core", ["ASP.NET Core", "ASPNET Core", "ASP.NETCore"]),
            new SkillAliasEntry(".NET", [".NET", "DotNet"]),
            new SkillAliasEntry("Entity Framework Core", ["Entity Framework Core", "EF Core"]),
            new SkillAliasEntry("PostgreSQL", ["PostgreSQL", "Postgres"]),
            new SkillAliasEntry("SQL Server", ["SQL Server", "Microsoft SQL Server", "MSSQL"]),
            new SkillAliasEntry("MongoDB", ["MongoDB", "Mongo"]),
            new SkillAliasEntry("Kubernetes", ["Kubernetes", "K8s"]),
            new SkillAliasEntry("REST API", ["REST API", "RESTful API", "RESTful Web API"]),
        });
    }
}

public sealed record SkillAliasEntry
{
    public string CanonicalName { get; }
    public IReadOnlyCollection<string> Aliases { get; }

    public SkillAliasEntry(string canonicalName, IEnumerable<string> aliases)
    {
        CanonicalName = canonicalName;
        Aliases = aliases.ToList().AsReadOnly();
    }
}

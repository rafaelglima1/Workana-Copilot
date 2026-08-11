using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Services;

public static class SkillMatcher
{
    public static SkillMatchEvidence Match(
        string requiredSkill,
        IReadOnlyCollection<SkillCapability> profileSkills,
        SkillAliasCatalog? catalog = null)
    {
        catalog ??= SkillAliasCatalog.Default;

        var normalizedRequired = SkillNameNormalizer.Normalize(requiredSkill);

        var exactMatch = profileSkills.FirstOrDefault(
            s => string.Equals(SkillNameNormalizer.Normalize(s.Name), normalizedRequired, StringComparison.OrdinalIgnoreCase));

        if (exactMatch is not null)
        {
            return SkillMatchEvidence.Create(
                requiredSkill,
                exactMatch.Name,
                exactMatch.Name,
                SkillMatchKind.Exact,
                exactMatch.Level,
                ScoreValue.From(SkillLevelMapping.ToScore(exactMatch.Level)));
        }

        var requiredCanonical = catalog.ResolveCanonical(requiredSkill);
        if (requiredCanonical is not null)
        {
            foreach (var profileSkill in profileSkills)
            {
                var profileCanonical = catalog.ResolveCanonical(profileSkill.Name);
                if (profileCanonical is not null &&
                    string.Equals(requiredCanonical, profileCanonical, StringComparison.OrdinalIgnoreCase))
                {
                    return SkillMatchEvidence.Create(
                        requiredSkill,
                        profileSkill.Name,
                        profileCanonical,
                        SkillMatchKind.Alias,
                        profileSkill.Level,
                        ScoreValue.From(SkillLevelMapping.ToScore(profileSkill.Level)));
                }
            }
        }

        return SkillMatchEvidence.NoMatch(requiredSkill);
    }

    public static IReadOnlyCollection<SkillMatchEvidence> MatchAll(
        IEnumerable<string> requiredSkills,
        IReadOnlyCollection<SkillCapability> profileSkills,
        SkillAliasCatalog? catalog = null)
    {
        catalog ??= SkillAliasCatalog.Default;

        return requiredSkills
            .Select(req => Match(req, profileSkills, catalog))
            .ToList()
            .AsReadOnly();
    }
}

using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record SkillCapability
{
    public string Name { get; }
    public SkillLevel Level { get; }

    private SkillCapability(string name, SkillLevel level)
    {
        Name = name;
        Level = level;
    }

    public static SkillCapability Create(string name, SkillLevel level)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Skill name is required.");

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            throw new DomainException("Skill name cannot be empty.");

        return new SkillCapability(trimmed, level);
    }
}

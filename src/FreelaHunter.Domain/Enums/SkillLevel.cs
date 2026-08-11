namespace FreelaHunter.Domain.Enums;

public enum SkillLevel
{
    None = 0,
    Basic = 1,
    Working = 2,
    Strong = 3,
    Expert = 4
}

public static class SkillLevelMapping
{
    public static int ToScore(SkillLevel level) => level switch
    {
        SkillLevel.None => 0,
        SkillLevel.Basic => 25,
        SkillLevel.Working => 50,
        SkillLevel.Strong => 75,
        SkillLevel.Expert => 100,
        _ => 0
    };
}

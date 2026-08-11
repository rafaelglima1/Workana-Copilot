namespace FreelaHunter.Domain.Enums;

public enum AiLeverageLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    VeryHigh = 4
}

public static class AiLeverageMapping
{
    public static int ToScore(AiLeverageLevel level) => level switch
    {
        AiLeverageLevel.None => 0,
        AiLeverageLevel.Low => 25,
        AiLeverageLevel.Medium => 50,
        AiLeverageLevel.High => 75,
        AiLeverageLevel.VeryHigh => 100,
        _ => 0
    };
}

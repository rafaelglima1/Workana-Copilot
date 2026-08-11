namespace FreelaHunter.Domain.Enums;

public enum DeliveryRiskLevel
{
    Low = 0,
    Moderate = 1,
    High = 2,
    Critical = 3
}

public static class DeliveryRiskMapping
{
    public static int ToConfidence(DeliveryRiskLevel level) => level switch
    {
        DeliveryRiskLevel.Low => 100,
        DeliveryRiskLevel.Moderate => 75,
        DeliveryRiskLevel.High => 40,
        DeliveryRiskLevel.Critical => 10,
        _ => 50
    };
}

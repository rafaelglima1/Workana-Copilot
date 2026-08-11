using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record OpportunityScoringContext
{
    public IReadOnlyCollection<SkillCapability> Skills { get; }
    public HourlyRateTarget TargetHourlyRate { get; }
    public decimal? EstimatedHours { get; }
    public DeliveryRiskLevel DeliveryRisk { get; }
    public AiLeverageLevel AiLeverage { get; }

    private OpportunityScoringContext(
        IReadOnlyCollection<SkillCapability> skills,
        HourlyRateTarget targetHourlyRate,
        decimal? estimatedHours,
        DeliveryRiskLevel deliveryRisk,
        AiLeverageLevel aiLeverage)
    {
        Skills = skills;
        TargetHourlyRate = targetHourlyRate;
        EstimatedHours = estimatedHours;
        DeliveryRisk = deliveryRisk;
        AiLeverage = aiLeverage;
    }

    public static OpportunityScoringContext Create(
        IEnumerable<SkillCapability> skills,
        HourlyRateTarget targetHourlyRate,
        decimal? estimatedHours,
        DeliveryRiskLevel deliveryRisk,
        AiLeverageLevel aiLeverage)
    {
        if (estimatedHours.HasValue && estimatedHours.Value <= 0)
            throw new DomainException("EstimatedHours must be greater than 0 when provided.");

        var distinctSkills = skills
            .GroupBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList()
            .AsReadOnly();

        return new OpportunityScoringContext(
            distinctSkills,
            targetHourlyRate,
            estimatedHours,
            deliveryRisk,
            aiLeverage);
    }
}

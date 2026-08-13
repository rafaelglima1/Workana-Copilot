using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Services;

public static class ProposalStrategist
{
    private const decimal PremiumRateMultiplier = 1.10m;
    private const decimal CompetitiveRateMultiplier = 0.90m;
    private const decimal DefaultHoursPerDay = 8m;
    private const int DefaultDaysPerWeek = 7;

    public static ProposalStrategy Calculate(
        OpportunityScore score,
        decimal estimatedHours,
        DeliveryRiskLevel deliveryRisk,
        FreelancerProfile profile,
        Budget budget)
    {
        var decision = DetermineDecision(score);
        var resolvedRate = profile.ResolveHourlyRate(budget.Currency);
        var recommendedRate = AdjustRate(score, resolvedRate, budget);
        var deliveryDays = CalculateDeliveryDays(estimatedHours, profile.WeeklyCapacityHours);
        var revenue = CalculateRevenue(recommendedRate.Amount, estimatedHours);
        var confidence = CalculateConfidence(score, deliveryRisk, estimatedHours, profile);
        var rationale = BuildRationale(score, estimatedHours, deliveryRisk, profile, budget, decision, recommendedRate);

        return ProposalStrategy.Create(
            decision,
            recommendedRate.Amount,
            recommendedRate.Currency,
            deliveryDays,
            revenue,
            confidence,
            rationale);
    }

    private static ProposalDecision DetermineDecision(OpportunityScore score)
    {
        if (score.Overall.Value >= 65)
            return ProposalDecision.Go;

        if (score.Overall.Value >= 50)
            return ProposalDecision.Maybe;

        return ProposalDecision.NoGo;
    }

    private static HourlyRateTarget AdjustRate(
        OpportunityScore score,
        HourlyRateTarget resolvedRate,
        Budget budget)
    {
        if (budget.Kind == BudgetKind.Unknown || budget.Kind == BudgetKind.Negotiable)
            return resolvedRate;

        decimal multiplier;
        if (score.Overall.Value >= 80)
            multiplier = PremiumRateMultiplier;
        else if (score.Overall.Value < 65)
            multiplier = CompetitiveRateMultiplier;
        else
            return resolvedRate;

        var adjustedAmount = Math.Round(resolvedRate.Amount * multiplier, 2, MidpointRounding.AwayFromZero);
        return HourlyRateTarget.Create(adjustedAmount, resolvedRate.Currency);
    }

    private static int CalculateDeliveryDays(decimal estimatedHours, decimal? weeklyCapacityHours)
    {
        if (estimatedHours <= 0)
            return 0;

        if (weeklyCapacityHours.HasValue && weeklyCapacityHours.Value > 0)
        {
            var hoursPerDay = weeklyCapacityHours.Value / DefaultDaysPerWeek;
            var days = Math.Ceiling(estimatedHours / hoursPerDay);
            return (int)days;
        }

        var defaultDays = Math.Ceiling(estimatedHours / DefaultHoursPerDay);
        return (int)defaultDays;
    }

    private static decimal CalculateRevenue(decimal recommendedRate, decimal estimatedHours)
    {
        if (estimatedHours <= 0)
            return 0;

        return Math.Round(recommendedRate * estimatedHours, 2, MidpointRounding.AwayFromZero);
    }

    private static ScoreValue CalculateConfidence(
        OpportunityScore score,
        DeliveryRiskLevel deliveryRisk,
        decimal estimatedHours,
        FreelancerProfile profile)
    {
        var scoreComponent = score.Overall.Value * 0.5m;
        var deliveryComponent = DeliveryRiskMapping.ToConfidence(deliveryRisk) * 0.3m;
        var capacityComponent = CalculateCapacityFit(profile.WeeklyCapacityHours, estimatedHours) * 0.2m;

        var total = scoreComponent + deliveryComponent + capacityComponent;
        return ScoreValue.From(Math.Round(total, 2, MidpointRounding.AwayFromZero));
    }

    private static decimal CalculateCapacityFit(decimal? weeklyCapacityHours, decimal estimatedHours)
    {
        if (!weeklyCapacityHours.HasValue || weeklyCapacityHours.Value <= 0)
            return 50;

        if (estimatedHours <= 0)
            return 50;

        var weeksNeeded = estimatedHours / weeklyCapacityHours.Value;

        if (weeksNeeded <= 1)
            return 100;

        if (weeksNeeded <= 2)
            return 80;

        if (weeksNeeded <= 4)
            return 60;

        return 30;
    }

    private static IReadOnlyCollection<string> BuildRationale(
        OpportunityScore score,
        decimal estimatedHours,
        DeliveryRiskLevel deliveryRisk,
        FreelancerProfile profile,
        Budget budget,
        ProposalDecision decision,
        HourlyRateTarget recommendedRate)
    {
        var reasons = new List<string>();

        reasons.Add($"Overall score: {score.Overall.Value}/100 → {decision}");

        reasons.Add($"Estimated effort: {estimatedHours}h");

        if (profile.WeeklyCapacityHours.HasValue)
            reasons.Add($"Weekly capacity: {profile.WeeklyCapacityHours.Value}h/week");
        else
            reasons.Add("Weekly capacity not configured — using 8h/day default");

        reasons.Add($"Recommended rate: {recommendedRate.Currency.Value} {recommendedRate.Amount}/h");

        if (budget.Currency is not null && budget.Currency.Value != recommendedRate.Currency.Value)
            reasons.Add($"Budget currency ({budget.Currency.Value}) differs from profile rate ({recommendedRate.Currency.Value})");

        if (deliveryRisk != DeliveryRiskLevel.Low)
            reasons.Add($"Delivery risk: {deliveryRisk}");

        if (score.TechnicalFit.Score.Value < 50)
            reasons.Add("Weak technical fit — consider upskilling");

        return reasons.AsReadOnly();
    }
}

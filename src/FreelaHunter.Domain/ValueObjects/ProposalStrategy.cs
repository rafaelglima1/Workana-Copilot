using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record ProposalStrategy
{
    public ProposalDecision Decision { get; }
    public decimal RecommendedHourlyRate { get; }
    public CurrencyCode RecommendedCurrency { get; }
    public int EstimatedDeliveryDays { get; }
    public decimal EstimatedRevenue { get; }
    public ScoreValue Confidence { get; }
    public IReadOnlyCollection<string> Rationale { get; }

    private ProposalStrategy(
        ProposalDecision decision,
        decimal recommendedHourlyRate,
        CurrencyCode recommendedCurrency,
        int estimatedDeliveryDays,
        decimal estimatedRevenue,
        ScoreValue confidence,
        IReadOnlyCollection<string> rationale)
    {
        Decision = decision;
        RecommendedHourlyRate = recommendedHourlyRate;
        RecommendedCurrency = recommendedCurrency;
        EstimatedDeliveryDays = estimatedDeliveryDays;
        EstimatedRevenue = estimatedRevenue;
        Confidence = confidence;
        Rationale = rationale;
    }

    public static ProposalStrategy Create(
        ProposalDecision decision,
        decimal recommendedHourlyRate,
        CurrencyCode recommendedCurrency,
        int estimatedDeliveryDays,
        decimal estimatedRevenue,
        ScoreValue confidence,
        IEnumerable<string>? rationale = null)
    {
        if (recommendedHourlyRate < 0)
            throw new DomainException("RecommendedHourlyRate cannot be negative.");

        if (estimatedDeliveryDays < 0)
            throw new DomainException("EstimatedDeliveryDays cannot be negative.");

        if (estimatedDeliveryDays > 365)
            throw new DomainException("EstimatedDeliveryDays cannot exceed 365.");

        if (estimatedRevenue < 0)
            throw new DomainException("EstimatedRevenue cannot be negative.");

        var reasons = rationale?.ToList() ?? [];

        return new ProposalStrategy(
            decision,
            recommendedHourlyRate,
            recommendedCurrency,
            estimatedDeliveryDays,
            estimatedRevenue,
            confidence,
            reasons);
    }
}

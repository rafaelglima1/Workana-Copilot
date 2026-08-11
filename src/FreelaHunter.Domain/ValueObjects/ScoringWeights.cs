using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record ScoringWeights
{
    public decimal TechnicalFit { get; }
    public decimal EconomicPotential { get; }
    public decimal ClientQuality { get; }
    public decimal Competition { get; }
    public decimal DeliveryConfidence { get; }
    public decimal AiLeverage { get; }

    public static ScoringWeights Default { get; } = new(
        technicalFit: 30,
        economicPotential: 25,
        clientQuality: 15,
        competition: 10,
        deliveryConfidence: 15,
        aiLeverage: 5);

    private ScoringWeights(
        decimal technicalFit,
        decimal economicPotential,
        decimal clientQuality,
        decimal competition,
        decimal deliveryConfidence,
        decimal aiLeverage)
    {
        TechnicalFit = technicalFit;
        EconomicPotential = economicPotential;
        ClientQuality = clientQuality;
        Competition = competition;
        DeliveryConfidence = deliveryConfidence;
        AiLeverage = aiLeverage;
    }

    public static ScoringWeights Create(
        decimal technicalFit,
        decimal economicPotential,
        decimal clientQuality,
        decimal competition,
        decimal deliveryConfidence,
        decimal aiLeverage)
    {
        if (technicalFit < 0 || economicPotential < 0 || clientQuality < 0 ||
            competition < 0 || deliveryConfidence < 0 || aiLeverage < 0)
            throw new DomainException("Scoring weights cannot be negative.");

        var total = technicalFit + economicPotential + clientQuality +
                    competition + deliveryConfidence + aiLeverage;

        if (total != 100)
            throw new DomainException($"Scoring weights must sum to 100. Got {total}.");

        return new ScoringWeights(technicalFit, economicPotential, clientQuality,
            competition, deliveryConfidence, aiLeverage);
    }

    public decimal WeightFor(string dimension) => dimension switch
    {
        nameof(TechnicalFit) => TechnicalFit,
        nameof(EconomicPotential) => EconomicPotential,
        nameof(ClientQuality) => ClientQuality,
        nameof(Competition) => Competition,
        nameof(DeliveryConfidence) => DeliveryConfidence,
        nameof(AiLeverage) => AiLeverage,
        _ => throw new DomainException($"Unknown dimension: {dimension}")
    };
}

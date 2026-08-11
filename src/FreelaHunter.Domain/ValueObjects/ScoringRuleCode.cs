namespace FreelaHunter.Domain.ValueObjects;

public enum ScoringRuleCode
{
    NoRequiredSkills,
    MissingRequiredSkill,
    StrongTechnicalFit,
    WeakTechnicalFit,

    BudgetUnknown,
    BudgetNegotiable,
    BudgetCurrencyMismatch,
    BudgetMonthlyNotConverted,
    EconomicExcellent,
    EconomicStrong,
    EconomicAcceptable,
    EconomicWeak,
    EconomicVeryWeak,

    ClientHistoryUnknown,
    ClientNew,
    ClientLowPaymentRatio,
    ClientStrongPaymentRatio,
    ClientRatingUnknown,
    ClientLowRating,
    ClientStrongRating,

    CompetitionUnknown,
    CompetitionLow,
    CompetitionModerate,
    CompetitionHigh,
    CompetitionVeryHigh,

    DeliveryLowRisk,
    DeliveryModerateRisk,
    DeliveryHighRisk,
    DeliveryCriticalRisk,

    AiLeverageNone,
    AiLeverageLow,
    AiLeverageMedium,
    AiLeverageHigh,
    AiLeverageVeryHigh
}

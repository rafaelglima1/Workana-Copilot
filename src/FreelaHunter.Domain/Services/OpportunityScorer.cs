using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Services;

public static class OpportunityScorer
{
    public static OpportunityScore Score(
        Opportunity opportunity,
        OpportunityScoringContext context,
        ScoringWeights? weights = null)
    {
        weights ??= ScoringWeights.Default;

        var technicalFit = CalculateTechnicalFit(opportunity, context);
        var technicalSkillMatches = GetTechnicalSkillMatches(opportunity, context);
        var economicPotential = CalculateEconomicPotential(opportunity, context);
        var clientQuality = CalculateClientQuality(opportunity);
        var competition = CalculateCompetition(opportunity);
        var deliveryConfidence = CalculateDeliveryConfidence(context);
        var aiLeverage = CalculateAiLeverage(context);

        var overallValue =
            technicalFit.WeightedContribution +
            economicPotential.WeightedContribution +
            clientQuality.WeightedContribution +
            competition.WeightedContribution +
            deliveryConfidence.WeightedContribution +
            aiLeverage.WeightedContribution;

        var overall = ScoreValue.From(Math.Round(overallValue, 2, MidpointRounding.AwayFromZero));

        return OpportunityScore.Create(
            overall,
            technicalFit,
            economicPotential,
            clientQuality,
            competition,
            deliveryConfidence,
            aiLeverage,
            technicalSkillMatches);
    }

    private static ScoreComponent CalculateTechnicalFit(Opportunity opportunity, OpportunityScoringContext context)
    {
        var rules = new List<ScoringRuleCode>();

        if (opportunity.RequiredSkills.Count == 0)
        {
            rules.Add(ScoringRuleCode.NoRequiredSkills);
            return ScoreComponent.Create(ScoreValue.From(50), ScoringWeights.Default.TechnicalFit, rules);
        }

        var totalScore = 0;
        foreach (var requiredSkill in opportunity.RequiredSkills)
        {
            var match = SkillMatcher.Match(requiredSkill, context.Skills);

            if (match.MatchKind == SkillMatchKind.NoMatch)
            {
                rules.Add(ScoringRuleCode.MissingRequiredSkill);
            }
            else
            {
                totalScore += SkillLevelMapping.ToScore(match.ProfileLevel!.Value);

                if (match.MatchKind == SkillMatchKind.Alias)
                    rules.Add(ScoringRuleCode.AliasSkillMatch);
                else
                    rules.Add(ScoringRuleCode.ExactSkillMatch);
            }
        }

        var average = (decimal)totalScore / opportunity.RequiredSkills.Count;
        var score = ScoreValue.From(Math.Round(average, 2, MidpointRounding.AwayFromZero));

        if (score >= 75)
            rules.Add(ScoringRuleCode.StrongTechnicalFit);
        else if (score < 50)
            rules.Add(ScoringRuleCode.WeakTechnicalFit);

        return ScoreComponent.Create(score, ScoringWeights.Default.TechnicalFit, rules);
    }

    private static IReadOnlyCollection<SkillMatchEvidence> GetTechnicalSkillMatches(
        Opportunity opportunity,
        OpportunityScoringContext context)
    {
        return SkillMatcher.MatchAll(opportunity.RequiredSkills, context.Skills);
    }

    private static ScoreComponent CalculateEconomicPotential(Opportunity opportunity, OpportunityScoringContext context)
    {
        var rules = new List<ScoringRuleCode>();
        var budget = opportunity.Budget;

        if (budget.Currency is not null && budget.Currency.Value != context.TargetHourlyRate.Currency.Value)
        {
            rules.Add(ScoringRuleCode.BudgetCurrencyMismatch);
            return ScoreComponent.Create(ScoreValue.From(50), ScoringWeights.Default.EconomicPotential, rules);
        }

        switch (budget.Kind)
        {
            case BudgetKind.Unknown:
                rules.Add(ScoringRuleCode.BudgetUnknown);
                return ScoreComponent.Create(ScoreValue.From(50), ScoringWeights.Default.EconomicPotential, rules);

            case BudgetKind.Negotiable:
                rules.Add(ScoringRuleCode.BudgetNegotiable);
                return ScoreComponent.Create(ScoreValue.From(50), ScoringWeights.Default.EconomicPotential, rules);
        }

        if (budget.Basis == BudgetBasis.Monthly)
        {
            rules.Add(ScoringRuleCode.BudgetMonthlyNotConverted);
            return ScoreComponent.Create(ScoreValue.From(50), ScoringWeights.Default.EconomicPotential, rules);
        }

        decimal? referenceAmount = budget.Kind switch
        {
            BudgetKind.Exact => budget.Amount,
            BudgetKind.Range => budget.Maximum,
            BudgetKind.UpTo => budget.Maximum,
            BudgetKind.From => budget.Minimum,
            _ => null
        };

        if (!referenceAmount.HasValue || referenceAmount.Value <= 0)
            return ScoreComponent.Create(ScoreValue.From(50), ScoringWeights.Default.EconomicPotential, rules);

        decimal effectiveHourlyRate;

        if (budget.Basis == BudgetBasis.FixedProject)
        {
            if (!context.EstimatedHours.HasValue || context.EstimatedHours.Value <= 0)
                throw new DomainException("EstimatedHours is required for FixedProject budget.");

            effectiveHourlyRate = referenceAmount.Value / context.EstimatedHours.Value;
        }
        else
        {
            effectiveHourlyRate = referenceAmount.Value;
        }

        var ratio = effectiveHourlyRate / context.TargetHourlyRate.Amount;
        var score = ScoreValue.From(RatioToScore(ratio));

        if (ratio >= 1.50m)
            rules.Add(ScoringRuleCode.EconomicExcellent);
        else if (ratio >= 1.00m)
            rules.Add(ScoringRuleCode.EconomicStrong);
        else if (ratio >= 0.60m)
            rules.Add(ScoringRuleCode.EconomicAcceptable);
        else if (ratio >= 0.40m)
            rules.Add(ScoringRuleCode.EconomicWeak);
        else
            rules.Add(ScoringRuleCode.EconomicVeryWeak);

        return ScoreComponent.Create(score, ScoringWeights.Default.EconomicPotential, rules);
    }

    private static decimal RatioToScore(decimal ratio) => ratio switch
    {
        >= 1.50m => 100,
        >= 1.25m => 90,
        >= 1.00m => 80,
        >= 0.80m => 60,
        >= 0.60m => 40,
        >= 0.40m => 20,
        _ => 5
    };

    private static ScoreComponent CalculateClientQuality(Opportunity opportunity)
    {
        var rules = new List<ScoringRuleCode>();
        var client = opportunity.Client;

        decimal paymentHistory;
        if (!client.PublishedProjects.HasValue || !client.PaidProjects.HasValue)
        {
            if (client.PublishedProjects.HasValue && client.PublishedProjects.Value == 0)
            {
                paymentHistory = 35;
                rules.Add(ScoringRuleCode.ClientNew);
            }
            else
            {
                paymentHistory = 35;
                rules.Add(ScoringRuleCode.ClientHistoryUnknown);
            }
        }
        else
        {
            if (client.PublishedProjects.Value == 0)
            {
                paymentHistory = 35;
                rules.Add(ScoringRuleCode.ClientNew);
            }
            else
            {
                var ratio = (decimal)client.PaidProjects.Value / client.PublishedProjects.Value;
                paymentHistory = Math.Round(ratio * 70, 2, MidpointRounding.AwayFromZero);

                if (ratio < 0.5m)
                    rules.Add(ScoringRuleCode.ClientLowPaymentRatio);
                else
                    rules.Add(ScoringRuleCode.ClientStrongPaymentRatio);
            }
        }

        decimal ratingComponent;
        if (client.Rating.HasValue)
        {
            ratingComponent = Math.Round((client.Rating.Value / 5m) * 30, 2, MidpointRounding.AwayFromZero);

            if (client.Rating.Value < 3m)
                rules.Add(ScoringRuleCode.ClientLowRating);
            else
                rules.Add(ScoringRuleCode.ClientStrongRating);
        }
        else
        {
            ratingComponent = 15;
            rules.Add(ScoringRuleCode.ClientRatingUnknown);
        }

        var total = paymentHistory + ratingComponent;
        var clamped = Math.Clamp(total, 0, 100);
        var score = ScoreValue.From(Math.Round(clamped, 2, MidpointRounding.AwayFromZero));

        return ScoreComponent.Create(score, ScoringWeights.Default.ClientQuality, rules);
    }

    private static ScoreComponent CalculateCompetition(Opportunity opportunity)
    {
        var rules = new List<ScoringRuleCode>();
        var competition = opportunity.Competition;

        decimal? proposalScore = competition.ProposalCount switch
        {
            null => 50,
            <= 5 => 100,
            <= 10 => 90,
            <= 20 => 75,
            <= 40 => 55,
            <= 60 => 35,
            <= 100 => 20,
            _ => 10
        };

        decimal? interestedScore = competition.InterestedFreelancerCount switch
        {
            null => 50,
            <= 10 => 100,
            <= 25 => 80,
            <= 50 => 60,
            <= 100 => 35,
            _ => 15
        };

        bool hasProposal = competition.ProposalCount.HasValue;
        bool hasInterested = competition.InterestedFreelancerCount.HasValue;

        decimal rawScore;
        if (hasProposal && hasInterested)
        {
            rawScore = proposalScore.Value * 0.70m + interestedScore.Value * 0.30m;
        }
        else if (hasProposal)
        {
            rawScore = proposalScore.Value;
        }
        else if (hasInterested)
        {
            rawScore = interestedScore.Value;
        }
        else
        {
            rawScore = 50;
            rules.Add(ScoringRuleCode.CompetitionUnknown);
        }

        if (rawScore >= 80)
            rules.Add(ScoringRuleCode.CompetitionLow);
        else if (rawScore >= 60)
            rules.Add(ScoringRuleCode.CompetitionModerate);
        else if (rawScore >= 35)
            rules.Add(ScoringRuleCode.CompetitionHigh);
        else
            rules.Add(ScoringRuleCode.CompetitionVeryHigh);

        var score = ScoreValue.From(Math.Round(rawScore, 2, MidpointRounding.AwayFromZero));
        return ScoreComponent.Create(score, ScoringWeights.Default.Competition, rules);
    }

    private static ScoreComponent CalculateDeliveryConfidence(OpportunityScoringContext context)
    {
        var rules = new List<ScoringRuleCode>();
        var rawScore = DeliveryRiskMapping.ToConfidence(context.DeliveryRisk);

        rules.Add(context.DeliveryRisk switch
        {
            DeliveryRiskLevel.Low => ScoringRuleCode.DeliveryLowRisk,
            DeliveryRiskLevel.Moderate => ScoringRuleCode.DeliveryModerateRisk,
            DeliveryRiskLevel.High => ScoringRuleCode.DeliveryHighRisk,
            DeliveryRiskLevel.Critical => ScoringRuleCode.DeliveryCriticalRisk,
            _ => ScoringRuleCode.DeliveryModerateRisk
        });

        var score = ScoreValue.From(rawScore);
        return ScoreComponent.Create(score, ScoringWeights.Default.DeliveryConfidence, rules);
    }

    private static ScoreComponent CalculateAiLeverage(OpportunityScoringContext context)
    {
        var rules = new List<ScoringRuleCode>();
        var rawScore = AiLeverageMapping.ToScore(context.AiLeverage);

        rules.Add(context.AiLeverage switch
        {
            AiLeverageLevel.None => ScoringRuleCode.AiLeverageNone,
            AiLeverageLevel.Low => ScoringRuleCode.AiLeverageLow,
            AiLeverageLevel.Medium => ScoringRuleCode.AiLeverageMedium,
            AiLeverageLevel.High => ScoringRuleCode.AiLeverageHigh,
            AiLeverageLevel.VeryHigh => ScoringRuleCode.AiLeverageVeryHigh,
            _ => ScoringRuleCode.AiLeverageNone
        });

        var score = ScoreValue.From(rawScore);
        return ScoreComponent.Create(score, ScoringWeights.Default.AiLeverage, rules);
    }
}

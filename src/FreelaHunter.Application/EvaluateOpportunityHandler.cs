using FreelaHunter.Application.Exceptions;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class EvaluateOpportunityHandler
{
    private readonly IOpportunityRepository _opportunityRepository;
    private readonly IFreelancerProfileRepository _profileRepository;
    private readonly IOpportunityIntelligenceGateway _gateway;

    public EvaluateOpportunityHandler(
        IOpportunityRepository opportunityRepository,
        IFreelancerProfileRepository profileRepository,
        IOpportunityIntelligenceGateway gateway)
    {
        _opportunityRepository = opportunityRepository;
        _profileRepository = profileRepository;
        _gateway = gateway;
    }

    public async Task<OpportunityEvaluation?> HandleAsync(
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        var id = OpportunityId.From(opportunityId);
        var opportunity = await _opportunityRepository.GetByIdAsync(id, cancellationToken);

        if (opportunity is null)
            return null;

        var profile = await _profileRepository.GetCurrentAsync(cancellationToken);

        if (profile is null)
            return null;

        var input = MapInput(opportunity);
        var analysis = await _gateway.AnalyzeAsync(input, cancellationToken);

        var preferredCurrency = opportunity.Budget.Currency;
        var resolvedRate = profile.ResolveHourlyRate(preferredCurrency);

        var context = OpportunityScoringContext.Create(
            profile.Skills,
            resolvedRate,
            analysis.EstimatedHours,
            analysis.DeliveryRisk,
            analysis.AiLeverage);

        var score = OpportunityScorer.Score(opportunity, context);

        var scoringInputs = BuildScoringInputSummary(profile, resolvedRate, analysis);

        return new OpportunityEvaluation
        {
            OpportunityId = id,
            Analysis = analysis,
            Score = score,
            ScoringInputs = scoringInputs
        };
    }

    private static ScoringInputSummary BuildScoringInputSummary(
        FreelancerProfile profile,
        HourlyRateTarget resolvedRate,
        OpportunitySemanticAnalysis analysis)
    {
        return new ScoringInputSummary
        {
            ProfileSkills = profile.Skills
                .Select(s => new SkillCapabilitySummary
                {
                    Name = s.Name,
                    Level = s.Level.ToString()
                })
                .ToList()
                .AsReadOnly(),
            TargetHourlyRate = new HourlyRateSummary
            {
                Amount = resolvedRate.Amount,
                Currency = resolvedRate.Currency.Value
            },
            EstimatedHours = analysis.EstimatedHours,
            DeliveryRisk = analysis.DeliveryRisk,
            AiLeverage = analysis.AiLeverage
        };
    }

    private static OpportunityIntelligenceInput MapInput(Domain.Entities.Opportunity opportunity)
    {
        var budgetSummary = FormatBudget(opportunity.Budget);
        var clientSummary = FormatClient(opportunity.Client);
        var competitionSummary = FormatCompetition(opportunity.Competition);

        return new OpportunityIntelligenceInput
        {
            Title = opportunity.Title,
            Description = opportunity.Description,
            Category = opportunity.Category,
            Subcategory = opportunity.Subcategory,
            RequiredSkills = opportunity.RequiredSkills.ToList().AsReadOnly(),
            Marketplace = opportunity.Marketplace.ToString(),
            BudgetSummary = budgetSummary,
            ClientSummary = clientSummary,
            CompetitionSummary = competitionSummary
        };
    }

    private static string FormatBudget(Budget budget)
    {
        var parts = new List<string>();

        if (budget.Kind == BudgetKind.Unknown)
            return "Unknown";

        if (budget.Currency is not null)
            parts.Add(budget.Currency.Value);

        parts.Add(budget.Basis.ToString());

        switch (budget.Kind)
        {
            case BudgetKind.Exact:
                parts.Add($"Fixed: {budget.Amount}");
                break;
            case BudgetKind.Range:
                parts.Add($"Range: {budget.Minimum}-{budget.Maximum}");
                break;
            case BudgetKind.UpTo:
                parts.Add($"Up to: {budget.Maximum}");
                break;
            case BudgetKind.From:
                parts.Add($"From: {budget.Minimum}");
                break;
            case BudgetKind.Negotiable:
                parts.Add("Negotiable");
                break;
        }

        return string.Join(" ", parts);
    }

    private static string FormatClient(ClientSnapshot client)
    {
        var parts = new List<string>();

        if (client.PublishedProjects.HasValue)
            parts.Add($"{client.PublishedProjects} published");

        if (client.PaidProjects.HasValue)
            parts.Add($"{client.PaidProjects} paid");

        if (client.Rating.HasValue)
            parts.Add($"rating {client.Rating}/5");

        return parts.Count > 0 ? string.Join(", ", parts) : "No data";
    }

    private static string FormatCompetition(CompetitionSnapshot competition)
    {
        var parts = new List<string>();

        if (competition.ProposalCount.HasValue)
            parts.Add($"{competition.ProposalCount} proposals");

        if (competition.InterestedFreelancerCount.HasValue)
            parts.Add($"{competition.InterestedFreelancerCount} interested");

        return parts.Count > 0 ? string.Join(", ", parts) : "No data";
    }
}

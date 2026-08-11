using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class AnalyzeOpportunityHandler
{
    private readonly IOpportunityRepository _repository;
    private readonly IOpportunityIntelligenceGateway _gateway;

    public AnalyzeOpportunityHandler(
        IOpportunityRepository repository,
        IOpportunityIntelligenceGateway gateway)
    {
        _repository = repository;
        _gateway = gateway;
    }

    public async Task<OpportunitySemanticAnalysis?> HandleAsync(
        Guid opportunityId,
        CancellationToken cancellationToken)
    {
        var id = OpportunityId.From(opportunityId);
        var opportunity = await _repository.GetByIdAsync(id, cancellationToken);

        if (opportunity is null)
            return null;

        var input = MapInput(opportunity);
        return await _gateway.AnalyzeAsync(input, cancellationToken);
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

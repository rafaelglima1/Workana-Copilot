namespace FreelaHunter.Application;

public sealed record OpportunityIntelligenceInput
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Subcategory { get; init; }
    public IReadOnlyCollection<string> RequiredSkills { get; init; } = [];
    public string? Marketplace { get; init; }
    public string? BudgetSummary { get; init; }
    public string? ClientSummary { get; init; }
    public string? CompetitionSummary { get; init; }
}

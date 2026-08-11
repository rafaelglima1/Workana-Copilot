namespace FreelaHunter.Contracts.Opportunities;

public sealed class CreateManualOpportunityRequest
{
    public ContractMarketplace Marketplace { get; set; }
    public string? ExternalReference { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public BudgetInput? Budget { get; set; }
    public ClientInput? Client { get; set; }
    public CompetitionInput? Competition { get; set; }
    public List<string>? RequiredSkills { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateOnly? ClientDeadline { get; set; }
}

public sealed class BudgetInput
{
    public ContractBudgetKind Kind { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public string? Currency { get; set; }
}

public sealed class ClientInput
{
    public string? DisplayName { get; set; }
    public string? Country { get; set; }
    public int? PublishedProjects { get; set; }
    public int? PaidProjects { get; set; }
    public decimal? Rating { get; set; }
}

public sealed class CompetitionInput
{
    public int? ProposalCount { get; set; }
    public int? InterestedFreelancerCount { get; set; }
}

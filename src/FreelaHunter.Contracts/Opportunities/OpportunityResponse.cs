namespace FreelaHunter.Contracts.Opportunities;

public sealed class OpportunityResponse
{
    public Guid Id { get; set; }
    public ContractMarketplace Marketplace { get; set; }
    public ContractIntakeChannel IntakeChannel { get; set; }
    public string? ExternalReference { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public BudgetResponse Budget { get; set; } = new();
    public ClientResponse Client { get; set; } = new();
    public CompetitionResponse Competition { get; set; } = new();
    public List<string> RequiredSkills { get; set; } = [];
    public DateTimeOffset? PublishedAt { get; set; }
    public DateOnly? ClientDeadline { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public ContractOpportunityStatus Status { get; set; }
}

public sealed class BudgetResponse
{
    public ContractBudgetKind Kind { get; set; }
    public ContractBudgetBasis Basis { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Minimum { get; set; }
    public decimal? Maximum { get; set; }
    public string? Currency { get; set; }
}

public sealed class ClientResponse
{
    public string? DisplayName { get; set; }
    public string? Country { get; set; }
    public int? PublishedProjects { get; set; }
    public int? PaidProjects { get; set; }
    public decimal? Rating { get; set; }
}

public sealed class CompetitionResponse
{
    public int? ProposalCount { get; set; }
    public int? InterestedFreelancerCount { get; set; }
}

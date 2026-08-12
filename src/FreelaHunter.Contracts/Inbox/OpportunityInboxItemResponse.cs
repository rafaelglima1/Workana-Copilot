using FreelaHunter.Contracts.Opportunities;

namespace FreelaHunter.Contracts.Inbox;

public sealed class OpportunityInboxItemResponse
{
    public Guid Id { get; set; }
    public ContractMarketplace Marketplace { get; set; }
    public ContractIntakeChannel IntakeChannel { get; set; }
    public string? ExternalReference { get; set; }
    public string Title { get; set; } = string.Empty;
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

using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application;

public sealed class OpportunityInboxItem
{
    public Guid Id { get; init; }
    public Marketplace Marketplace { get; init; }
    public IntakeChannel IntakeChannel { get; init; }
    public string? ExternalReference { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Subcategory { get; init; }
    public string BudgetKind { get; init; } = string.Empty;
    public string BudgetBasis { get; init; } = string.Empty;
    public decimal? BudgetAmount { get; init; }
    public decimal? BudgetMinimum { get; init; }
    public decimal? BudgetMaximum { get; init; }
    public string? BudgetCurrency { get; init; }
    public string? ClientDisplayName { get; init; }
    public string? ClientCountry { get; init; }
    public int? ClientPublishedProjects { get; init; }
    public int? ClientPaidProjects { get; init; }
    public decimal? ClientRating { get; init; }
    public int? CompetitionProposalCount { get; init; }
    public int? CompetitionInterestedFreelancerCount { get; init; }
    public IReadOnlyCollection<string> RequiredSkills { get; init; } = [];
    public DateTimeOffset? PublishedAt { get; init; }
    public DateOnly? ClientDeadline { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public OpportunityStatus Status { get; init; }
}

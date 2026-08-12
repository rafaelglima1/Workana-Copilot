namespace FreelaHunter.Infrastructure.Persistence.Models;

public sealed class OpportunityRecord
{
    public Guid Id { get; set; }
    public string Marketplace { get; set; } = string.Empty;
    public string IntakeChannel { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Subcategory { get; set; }
    public string BudgetKind { get; set; } = string.Empty;
    public string BudgetBasis { get; set; } = string.Empty;
    public decimal? BudgetAmount { get; set; }
    public decimal? BudgetMinimum { get; set; }
    public decimal? BudgetMaximum { get; set; }
    public string? BudgetCurrency { get; set; }
    public string? ClientDisplayName { get; set; }
    public string? ClientCountry { get; set; }
    public int? ClientPublishedProjects { get; set; }
    public int? ClientPaidProjects { get; set; }
    public decimal? ClientRating { get; set; }
    public int? CompetitionProposalCount { get; set; }
    public int? CompetitionInterestedFreelancerCount { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateOnly? ClientDeadline { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public ICollection<OpportunitySkillRecord> Skills { get; set; } = new List<OpportunitySkillRecord>();
}

public sealed class OpportunitySkillRecord
{
    public Guid OpportunityId { get; set; }
    public string Skill { get; set; } = string.Empty;
    public OpportunityRecord Opportunity { get; set; } = null!;
}

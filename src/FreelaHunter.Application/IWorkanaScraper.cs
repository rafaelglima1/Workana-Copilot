namespace FreelaHunter.Application;

public interface IWorkanaScraper
{
    Task<IReadOnlyList<ScrapedOpportunity>> ScrapeAsync(
        int maxPages,
        CancellationToken cancellationToken);
}

public sealed record ScrapedOpportunity
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? Category { get; init; }
    public string? Subcategory { get; init; }
    public required List<string> RequiredSkills { get; init; }
    public string? BudgetSummary { get; init; }
    public string? ClientCountry { get; init; }
    public int? ClientJobsPosted { get; init; }
    public decimal? ClientHireRate { get; init; }
    public int? ProposalsCount { get; init; }
    public string? ExternalUrl { get; init; }
    public DateTime? PublishedAt { get; init; }
}

using FreelaHunter.Application.Providers;

namespace FreelaHunter.Application;

[Obsolete("Use IJobProvider from FreelaHunter.Application.Providers instead.")]
public interface IWorkanaScraper
{
    Task<IReadOnlyList<ScrapedOpportunity>> ScrapeAsync(
        int maxPages,
        CancellationToken cancellationToken);
}

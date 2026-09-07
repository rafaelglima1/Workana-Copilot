using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application.Providers;

public interface IJobProvider
{
    string ProviderId { get; }

    string DisplayName { get; }

    Marketplace Marketplace { get; }

    Task<IReadOnlyList<ScrapedOpportunity>> FetchOpenJobsAsync(
        int maxPages,
        CancellationToken cancellationToken = default);

    Task<ScrapedOpportunity?> ExtractJobDetailsAsync(
        string jobIdOrUrl,
        CancellationToken cancellationToken = default);

    Task<bool> SubmitApplicationAsync(
        JobApplicationData applicationData,
        CancellationToken cancellationToken = default);
}

using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Inbox;
using FreelaHunter.Contracts.Opportunities;

namespace FreelaHunter.Web.Services;

public sealed class FreelaHunterApiClient
{
    private readonly HttpClient _httpClient;

    public FreelaHunterApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedOpportunityResponse?> GetInboxAsync(
        int page = 1,
        int pageSize = 25,
        string? search = null,
        string? status = null,
        string? sort = null,
        string? recommendation = null,
        CancellationToken cancellationToken = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrEmpty(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        if (!string.IsNullOrEmpty(status))
            query += $"&status={status}";

        if (!string.IsNullOrEmpty(sort))
            query += $"&sort={sort}";

        if (!string.IsNullOrEmpty(recommendation))
            query += $"&recommendation={recommendation}";

        return await _httpClient.GetFromJsonAsync<PagedOpportunityResponse>(
            $"/api/opportunities{query}", cancellationToken);
    }

    public async Task<OpportunityResponse?> GetOpportunityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<OpportunityResponse>(
            $"/api/opportunities/{id}", cancellationToken);
    }

    public async Task<OpportunityResponse?> CreateManualOpportunityAsync(
        CreateManualOpportunityRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/opportunities/manual", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OpportunityResponse>(cancellationToken);
    }

    public async Task<OpportunityResponse?> ShortlistOpportunityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/opportunities/{id}/shortlist", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OpportunityResponse>(cancellationToken);
    }

    public async Task<OpportunityResponse?> IgnoreOpportunityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/opportunities/{id}/ignore", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OpportunityResponse>(cancellationToken);
    }

    public async Task<OpportunityResponse?> ArchiveOpportunityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/opportunities/{id}/archive", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OpportunityResponse>(cancellationToken);
    }

    public async Task<OpportunityEvaluationResponse?> EvaluateOpportunityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/opportunities/{id}/evaluate", new { }, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OpportunityEvaluationResponse>(cancellationToken);
    }

    public async Task<OpportunityEvaluationResponse?> GetEvaluationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<OpportunityEvaluationResponse>(
            $"/api/opportunities/{id}/evaluation", cancellationToken);
    }

    public async Task<ProposalStrategyResponse?> GenerateProposalStrategyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/opportunities/{id}/proposal-strategy", new { }, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProposalStrategyResponse>(cancellationToken);
    }

    public async Task<FreelancerProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<FreelancerProfileResponse>(
            "/api/freelancer-profile", cancellationToken);
    }

    public async Task<FreelancerProfileResponse?> SaveProfileAsync(
        SaveFreelancerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/freelancer-profile", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FreelancerProfileResponse>(cancellationToken);
    }

    public async Task<ImportOpportunitiesResponse?> ImportOpportunitiesAsync(
        ImportOpportunitiesRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/opportunities/import", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ImportOpportunitiesResponse>(cancellationToken);
    }

    public async Task<PagedOpportunityResponse?> SearchOpportunitiesAsync(
        OpportunitySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = $"?page={request.Page}&pageSize={request.PageSize}";

        if (!string.IsNullOrEmpty(request.Search))
            query += $"&search={Uri.EscapeDataString(request.Search)}";

        if (!string.IsNullOrEmpty(request.Recommendation))
            query += $"&recommendation={request.Recommendation}";

        if (!string.IsNullOrEmpty(request.Sort))
            query += $"&sort={request.Sort}";

        return await _httpClient.GetFromJsonAsync<PagedOpportunityResponse>(
            $"/api/opportunities{query}", cancellationToken);
    }

    public async Task<SettingsResponse?> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<SettingsResponse>("/api/settings", cancellationToken);
    }

    public async Task UpdateSettingsAsync(UpdateSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/settings", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ScraperRunResult?> RunScraperAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/scraper/run", new { }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ScraperRunResult>(cancellationToken);
    }
}

public sealed class OpportunitySearchRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Search { get; init; }
    public string? Recommendation { get; init; }
    public string? Sort { get; init; }
}

public sealed class SettingsResponse
{
    public LlmSettingsResponse? Llm { get; set; }
    public TelegramSettingsResponse? Telegram { get; set; }
    public ScraperSettingsResponse? Scraper { get; set; }
}

public sealed class LlmSettingsResponse
{
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public bool HasApiKey { get; set; }
}

public sealed class TelegramSettingsResponse
{
    public string BotToken { get; set; } = "";
    public string ChatId { get; set; } = "";
    public bool Enabled { get; set; }
}

public sealed class ScraperSettingsResponse
{
    public string WorkanaBaseUrl { get; set; } = "";
    public string WorkanaLanguage { get; set; } = "pt";
    public string WorkanaSkills { get; set; } = "";
    public string WorkanaSort { get; set; } = "date";
    public bool WorkanaLoginEnabled { get; set; }
    public string WorkanaEmail { get; set; } = "";
    public string NerdinBaseUrl { get; set; } = "";
    public string NerdinTechnology { get; set; } = "";
    public string[] EnabledProviders { get; set; } = ["workana"];
    public int ScrapeIntervalMinutes { get; set; }
    public int MaxPages { get; set; }
    public bool CamoufoxEnabled { get; set; }
    public string CamoufoxServerUrl { get; set; } = "";
}

public sealed class UpdateSettingsRequest
{
    public LlmUpdateRequest? Llm { get; set; }
    public TelegramUpdateRequest? Telegram { get; set; }
    public ScraperUpdateRequest? Scraper { get; set; }
}

public sealed class LlmUpdateRequest
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public string? ApiKey { get; set; }
}

public sealed class TelegramUpdateRequest
{
    public string? BotToken { get; set; }
    public string? ChatId { get; set; }
    public bool? Enabled { get; set; }
}

public sealed class ScraperUpdateRequest
{
    public string? WorkanaBaseUrl { get; set; }
    public string? WorkanaLanguage { get; set; }
    public string? WorkanaSkills { get; set; }
    public string? WorkanaSort { get; set; }
    public bool? WorkanaLoginEnabled { get; set; }
    public string? WorkanaEmail { get; set; }
    public string? WorkanaPassword { get; set; }
    public string? NerdinBaseUrl { get; set; }
    public string? NerdinTechnology { get; set; }
    public string[]? EnabledProviders { get; set; }
    public int? ScrapeIntervalMinutes { get; set; }
    public int? MaxPages { get; set; }
    public bool? CamoufoxEnabled { get; set; }
    public string? CamoufoxServerUrl { get; set; }
}

public sealed class ScraperRunResult
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Total { get; set; }
}

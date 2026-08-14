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
        CancellationToken cancellationToken = default)
    {
        var query = $"?page={page}&pageSize={pageSize}";

        if (!string.IsNullOrEmpty(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        if (!string.IsNullOrEmpty(status))
            query += $"&status={status}";

        if (!string.IsNullOrEmpty(sort))
            query += $"&sort={sort}";

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
}

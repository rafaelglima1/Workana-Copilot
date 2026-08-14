using FreelaHunter.Contracts.Opportunities;

namespace FreelaHunter.Contracts.Opportunities;

public sealed class ImportOpportunitiesRequest
{
    public required List<CreateManualOpportunityRequest> Items { get; init; }
}

public sealed class ImportOpportunitiesResponse
{
    public int Imported { get; init; }
    public int Failed { get; init; }
    public List<ImportErrorDetail> Errors { get; init; } = [];
}

public sealed class ImportErrorDetail
{
    public int Index { get; init; }
    public string? Title { get; init; }
    public string Error { get; init; } = string.Empty;
}

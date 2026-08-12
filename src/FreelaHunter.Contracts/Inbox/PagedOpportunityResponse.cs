namespace FreelaHunter.Contracts.Inbox;

public sealed class PagedOpportunityResponse
{
    public List<OpportunityInboxItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

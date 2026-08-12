using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application;

public sealed class OpportunityQueryCriteria
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public string? Search { get; init; }
    public Marketplace? Marketplace { get; init; }
    public IntakeChannel? IntakeChannel { get; init; }
    public OpportunityStatus? Status { get; init; }
    public DateTimeOffset? CapturedFrom { get; init; }
    public DateTimeOffset? CapturedTo { get; init; }
    public OpportunitySort Sort { get; init; } = OpportunitySort.CapturedAtDesc;
    public bool? Evaluated { get; init; }
    public OpportunityRecommendation? Recommendation { get; init; }
    public decimal? MinScore { get; init; }
    public decimal? MaxScore { get; init; }
}

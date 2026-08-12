using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application;

public class ListOpportunitiesHandler
{
    private readonly IOpportunityQueryRepository _queryRepository;

    public ListOpportunitiesHandler(IOpportunityQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public async Task<PagedResult<OpportunityInboxItem>> HandleAsync(
        OpportunityQueryCriteria criteria,
        CancellationToken cancellationToken)
    {
        var page = criteria.Page < 1
            ? throw new ArgumentOutOfRangeException(nameof(criteria), "Page must be >= 1.")
            : criteria.Page;

        var pageSize = criteria.PageSize < 1 || criteria.PageSize > 100
            ? throw new ArgumentOutOfRangeException(nameof(criteria), "PageSize must be between 1 and 100.")
            : criteria.PageSize;

        if (criteria.CapturedFrom.HasValue && criteria.CapturedTo.HasValue &&
            criteria.CapturedFrom > criteria.CapturedTo)
        {
            throw new ArgumentOutOfRangeException(nameof(criteria),
                "CapturedFrom must not be greater than CapturedTo.");
        }

        var normalized = new OpportunityQueryCriteria
        {
            Page = page,
            PageSize = pageSize,
            Search = NormalizeSearch(criteria.Search),
            Marketplace = criteria.Marketplace,
            IntakeChannel = criteria.IntakeChannel,
            Status = criteria.Status,
            CapturedFrom = criteria.CapturedFrom,
            CapturedTo = criteria.CapturedTo,
            Sort = criteria.Sort
        };

        return await _queryRepository.SearchAsync(normalized, cancellationToken);
    }

    private static string? NormalizeSearch(string? search)
    {
        if (search is null)
            return null;

        var trimmed = search.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}

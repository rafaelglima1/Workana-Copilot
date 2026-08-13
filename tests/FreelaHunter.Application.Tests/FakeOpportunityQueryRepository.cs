using FreelaHunter.Application;

namespace FreelaHunter.Application.Tests;

internal sealed class FakeOpportunityQueryRepository : IOpportunityQueryRepository
{
    public List<OpportunityQueryCriteria> ReceivedCriteria { get; } = [];
    private Func<OpportunityQueryCriteria, CancellationToken, Task<PagedResult<OpportunityInboxItem>>> _handler;
    private IReadOnlyCollection<OpportunityInboxItem>? _items;

    public FakeOpportunityQueryRepository()
    {
        _handler = (criteria, ct) =>
        {
            ReceivedCriteria.Add(criteria);
            return Task.FromResult(new PagedResult<OpportunityInboxItem>
            {
                Items = _items?.ToList() ?? [],
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                TotalCount = _items?.Count ?? 0,
                TotalPages = _items?.Count > 0 ? 1 : 0
            });
        };
    }

    public FakeOpportunityQueryRepository(
        Func<OpportunityQueryCriteria, CancellationToken, Task<PagedResult<OpportunityInboxItem>>> handler)
    {
        _handler = handler;
    }

    public void SetResult(IReadOnlyCollection<OpportunityInboxItem> items)
    {
        _items = items;
    }

    public Task<PagedResult<OpportunityInboxItem>> SearchAsync(
        OpportunityQueryCriteria criteria,
        CancellationToken cancellationToken)
    {
        return _handler(criteria, cancellationToken);
    }
}

using FreelaHunter.Application;

namespace FreelaHunter.Application.Tests;

internal sealed class FakeOpportunityQueryRepository : IOpportunityQueryRepository
{
    public List<OpportunityQueryCriteria> ReceivedCriteria { get; } = [];
    private readonly Func<OpportunityQueryCriteria, CancellationToken, Task<PagedResult<OpportunityInboxItem>>> _handler;

    public FakeOpportunityQueryRepository()
    {
        _handler = (criteria, ct) =>
        {
            ReceivedCriteria.Add(criteria);
            return Task.FromResult(new PagedResult<OpportunityInboxItem>
            {
                Items = [],
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                TotalCount = 0,
                TotalPages = 0
            });
        };
    }

    public FakeOpportunityQueryRepository(
        Func<OpportunityQueryCriteria, CancellationToken, Task<PagedResult<OpportunityInboxItem>>> handler)
    {
        _handler = handler;
    }

    public Task<PagedResult<OpportunityInboxItem>> SearchAsync(
        OpportunityQueryCriteria criteria,
        CancellationToken cancellationToken)
    {
        return _handler(criteria, cancellationToken);
    }
}

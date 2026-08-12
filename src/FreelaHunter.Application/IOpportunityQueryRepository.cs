namespace FreelaHunter.Application;

public interface IOpportunityQueryRepository
{
    Task<PagedResult<OpportunityInboxItem>> SearchAsync(
        OpportunityQueryCriteria criteria,
        CancellationToken cancellationToken);
}

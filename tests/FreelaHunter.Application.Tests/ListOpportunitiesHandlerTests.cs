using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application.Tests;

public class ListOpportunitiesHandlerTests
{
    [Fact]
    public async Task HandleAsync_DefaultsPage1PageSize25()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await handler.HandleAsync(new OpportunityQueryCriteria(), CancellationToken.None);

        Assert.Single(queryRepo.ReceivedCriteria);
        Assert.Equal(1, queryRepo.ReceivedCriteria[0].Page);
        Assert.Equal(25, queryRepo.ReceivedCriteria[0].PageSize);
    }

    [Fact]
    public async Task HandleAsync_NormalizesEmptySearchToNull()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await handler.HandleAsync(new OpportunityQueryCriteria { Search = "  " }, CancellationToken.None);

        Assert.Null(queryRepo.ReceivedCriteria[0].Search);
    }

    [Fact]
    public async Task HandleAsync_TrimSearch()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await handler.HandleAsync(new OpportunityQueryCriteria { Search = "  hello  " }, CancellationToken.None);

        Assert.Equal("hello", queryRepo.ReceivedCriteria[0].Search);
    }

    [Fact]
    public async Task HandleAsync_PageBelow1_Throws()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new OpportunityQueryCriteria { Page = 0 }, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PageSize101_Throws()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new OpportunityQueryCriteria { PageSize = 200 }, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PageSize0_Throws()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new OpportunityQueryCriteria { PageSize = 0 }, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_CapturedFromAfterCapturedTo_Throws()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.HandleAsync(new OpportunityQueryCriteria
            {
                CapturedFrom = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero),
                CapturedTo = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_PassesFiltersCorrectly()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        var criteria = new OpportunityQueryCriteria
        {
            Page = 2,
            PageSize = 10,
            Search = "test",
            Marketplace = Marketplace.Workana,
            Status = OpportunityStatus.New,
            Sort = OpportunitySort.TitleAsc
        };

        await handler.HandleAsync(criteria, CancellationToken.None);

        var received = queryRepo.ReceivedCriteria[0];
        Assert.Equal(2, received.Page);
        Assert.Equal(10, received.PageSize);
        Assert.Equal("test", received.Search);
        Assert.Equal(Marketplace.Workana, received.Marketplace);
        Assert.Equal(OpportunityStatus.New, received.Status);
        Assert.Equal(OpportunitySort.TitleAsc, received.Sort);
    }

    [Fact]
    public async Task HandleAsync_DoesNotCallIntelligenceGateway()
    {
        var queryRepo = new FakeOpportunityQueryRepository();
        var handler = new ListOpportunitiesHandler(queryRepo);

        await handler.HandleAsync(new OpportunityQueryCriteria(), CancellationToken.None);

        Assert.Single(queryRepo.ReceivedCriteria);
    }

    [Fact]
    public async Task HandleAsync_ReturnsResultFromQueryRepo()
    {
        var expectedItems = new List<OpportunityInboxItem>
        {
            new() { Id = Guid.NewGuid(), Title = "Test" }
        };
        var queryRepo = new FakeOpportunityQueryRepository((criteria, ct) =>
            Task.FromResult(new PagedResult<OpportunityInboxItem>
            {
                Items = expectedItems,
                Page = 1,
                PageSize = 25,
                TotalCount = 1,
                TotalPages = 1
            }));
        var handler = new ListOpportunitiesHandler(queryRepo);

        var result = await handler.HandleAsync(new OpportunityQueryCriteria(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Test", result.Items.First().Title);
        Assert.Equal(1, result.TotalCount);
    }
}

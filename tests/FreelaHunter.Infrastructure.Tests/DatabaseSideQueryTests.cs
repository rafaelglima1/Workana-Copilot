using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FreelaHunter.Infrastructure.Tests;

public class DatabaseSideQueryTests
{
    private static Opportunity CreateOpportunity(
        string title = "Test",
        DateTimeOffset? capturedAt = null,
        IEnumerable<string>? skills = null)
    {
        return Opportunity.Rehydrate(
            id: OpportunityId.New(),
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            externalReference: null,
            title: title,
            description: "Description",
            category: null,
            subcategory: null,
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL")),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            requiredSkills: skills?.ToList().AsReadOnly() ?? new List<string> { "C#" }.AsReadOnly(),
            publishedAt: DateTimeOffset.UtcNow,
            clientDeadline: null,
            capturedAt: capturedAt ?? DateTimeOffset.UtcNow,
            status: OpportunityStatus.New);
    }

    private static (FreelaHunterDbContext context, SqliteConnection connection, QueryCounter counter) CreateCountingContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var counter = new QueryCounter();

        var options = new DbContextOptionsBuilder<FreelaHunterDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(counter)
            .Options;

        var context = new FreelaHunterDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection, counter);
    }

    [Fact]
    public async Task SearchAsync_ExecutesMaximumThreeQueries()
    {
        var (context, connection, counter) = CreateCountingContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        for (var i = 0; i < 20; i++)
        {
            await oppRepo.AddAsync(CreateOpportunity(title: $"Opp {i}"), CancellationToken.None);
        }

        counter.Reset();

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { PageSize = 10 }, CancellationToken.None);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(20, result.TotalCount);
        Assert.True(counter.Count <= 3, $"Expected at most 3 queries, got {counter.Count}");
    }

    [Fact]
    public async Task SearchAsync_SkillsSearchDoesNotCauseNPlusOne()
    {
        var (context, connection, counter) = CreateCountingContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        for (var i = 0; i < 10; i++)
        {
            await oppRepo.AddAsync(
                CreateOpportunity(title: $"Opp {i}", skills: [$"SkillA{i}", $"SkillB{i}"]),
                CancellationToken.None);
        }

        counter.Reset();

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "SkillA5", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Contains("SkillA5", result.Items.First().RequiredSkills);
        Assert.True(counter.Count <= 3, $"Expected at most 3 queries for skills search, got {counter.Count}");
    }

    [Fact]
    public async Task SearchAsync_DateFilterWithDifferentOffsets_SameUtcInstant()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);

        // Same UTC instant: 2025-06-01 12:00:00 UTC
        var utc = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var brt = new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.FromHours(-3)); // BRT = UTC-3
        var jst = new DateTimeOffset(2025, 6, 1, 21, 0, 0, TimeSpan.FromHours(9)); // JST = UTC+9

        await oppRepo.AddAsync(CreateOpportunity(title: "UTC", capturedAt: utc), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "BRT", capturedAt: brt), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "JST", capturedAt: jst), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);

        // Filter from 2025-06-01 11:00 UTC - should include all three
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria
            {
                CapturedFrom = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero),
                CapturedTo = new DateTimeOffset(2025, 6, 1, 13, 0, 0, TimeSpan.Zero),
                PageSize = 100
            },
            CancellationToken.None);

        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task SearchAsync_DateFilterExcludesByUtcNotLocal()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);

        // 2025-06-01 02:00 UTC = 2025-05-31 23:00 BRT (previous day in BRT)
        var beforeUtc = new DateTimeOffset(2025, 6, 1, 1, 0, 0, TimeSpan.Zero); // 01:00 UTC
        var afterUtc = new DateTimeOffset(2025, 6, 1, 3, 0, 0, TimeSpan.Zero);  // 03:00 UTC

        await oppRepo.AddAsync(CreateOpportunity(title: "Before", capturedAt: beforeUtc), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "After", capturedAt: afterUtc), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);

        // Filter from 02:00 UTC - should only include "After"
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria
            {
                CapturedFrom = new DateTimeOffset(2025, 6, 1, 2, 0, 0, TimeSpan.Zero),
                PageSize = 100
            },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("After", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_LargeDataset_DbSidePagination()
    {
        var (context, connection, counter) = CreateCountingContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        for (var i = 0; i < 150; i++)
        {
            var capturedAt = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).AddHours(i);
            await oppRepo.AddAsync(
                CreateOpportunity(title: $"Opp {i:D3}", capturedAt: capturedAt),
                CancellationToken.None);
        }

        counter.Reset();

        var queryRepo = new EfOpportunityQueryRepository(context);

        // Page 1
        var page1 = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Page = 1, PageSize = 20, Sort = OpportunitySort.CapturedAtDesc },
            CancellationToken.None);

        Assert.Equal(20, page1.Items.Count);
        Assert.Equal(150, page1.TotalCount);
        Assert.Equal(8, page1.TotalPages);

        // Most recent first
        Assert.Equal("Opp 149", page1.Items.First().Title);
        Assert.Equal("Opp 130", page1.Items.Last().Title);

        // Page 8 (last page)
        var page8 = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Page = 8, PageSize = 20, Sort = OpportunitySort.CapturedAtDesc },
            CancellationToken.None);

        Assert.Equal(10, page8.Items.Count);
        Assert.Equal("Opp 009", page8.Items.First().Title);
        Assert.Equal("Opp 000", page8.Items.Last().Title);

        // Queries: count + paged data + skills = 3 per page, total 6
        Assert.True(counter.Count <= 6, $"Expected at most 6 queries for 2 pages, got {counter.Count}");
    }

    [Fact]
    public async Task SearchAsync_PublishedAtDesc_PutsNullsLast()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var oppWithDate = CreateOpportunity(title: "With Date");
        var oppWithoutDate = Opportunity.Rehydrate(
            OpportunityId.New(), Marketplace.Workana, IntakeChannel.Manual, null,
            "No Date", "Description", null, null,
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            [], null, null, DateTimeOffset.UtcNow, OpportunityStatus.New);

        await oppRepo.AddAsync(oppWithDate, CancellationToken.None);
        await oppRepo.AddAsync(oppWithoutDate, CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Sort = OpportunitySort.PublishedAtDesc, PageSize = 100 },
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.NotNull(result.Items.First().PublishedAt);
        Assert.Null(result.Items.Last().PublishedAt);
    }

    [Fact]
    public async Task SearchAsync_PublishedAtAsc_PutsNullsLast()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var oppWithDate = CreateOpportunity(title: "With Date");
        var oppWithoutDate = Opportunity.Rehydrate(
            OpportunityId.New(), Marketplace.Workana, IntakeChannel.Manual, null,
            "No Date", "Description", null, null,
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            [], null, null, DateTimeOffset.UtcNow, OpportunityStatus.New);

        await oppRepo.AddAsync(oppWithDate, CancellationToken.None);
        await oppRepo.AddAsync(oppWithoutDate, CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Sort = OpportunitySort.PublishedAtAsc, PageSize = 100 },
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.NotNull(result.Items.First().PublishedAt);
        Assert.Null(result.Items.Last().PublishedAt);
    }

    [Fact]
    public async Task SearchAsync_CapturedAtRoundtrip_PreservesUtc()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var original = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.FromHours(-3)); // BRT
        await oppRepo.AddAsync(CreateOpportunity(title: "Roundtrip", capturedAt: original), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { PageSize = 100 }, CancellationToken.None);

        var item = result.Items.First();
        // The UTC instant should be preserved, but stored/reconstructed as UTC
        Assert.Equal(original.UtcDateTime, item.CapturedAt.UtcDateTime);
        Assert.Equal(TimeSpan.Zero, item.CapturedAt.Offset);
    }

    [Fact]
    public async Task SearchAsync_CombinedFiltersAndSort()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);

        await oppRepo.AddAsync(CreateOpportunity(title: "Alpha", capturedAt: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "Beta", capturedAt: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "Gamma", capturedAt: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria
            {
                CapturedFrom = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero),
                CapturedTo = new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero),
                Sort = OpportunitySort.TitleAsc,
                PageSize = 100
            },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Beta", result.Items.First().Title);
    }
}

internal sealed class QueryCounter : DbCommandInterceptor
{
    private int _count;

    public int Count => _count;

    public void Reset() => _count = 0;

    public override InterceptionResult<int> NonQueryExecuting(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        _count++;
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _count++;
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        _count++;
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        System.Data.Common.DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        _count++;
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}

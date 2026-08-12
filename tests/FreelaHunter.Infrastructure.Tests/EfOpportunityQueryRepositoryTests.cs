using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;

namespace FreelaHunter.Infrastructure.Tests;

public class EfOpportunityQueryRepositoryTests
{
    private static Opportunity CreateOpportunity(
        string title = "Test",
        Marketplace marketplace = Marketplace.Workana,
        DateTimeOffset? capturedAt = null,
        OpportunityStatus status = OpportunityStatus.New,
        string? category = null,
        string? subcategory = null,
        string? externalReference = null,
        string? clientDisplayName = null,
        IEnumerable<string>? skills = null)
    {
        return Opportunity.Rehydrate(
            id: OpportunityId.New(),
            marketplace: marketplace,
            intakeChannel: IntakeChannel.Manual,
            externalReference: externalReference,
            title: title,
            description: "Description",
            category: category,
            subcategory: subcategory,
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL")),
            client: ClientSnapshot.Create(displayName: clientDisplayName, country: "BR"),
            competition: CompetitionSnapshot.Create(10, 20),
            requiredSkills: skills?.ToList().AsReadOnly() ?? new List<string> { "C#" }.AsReadOnly(),
            publishedAt: DateTimeOffset.UtcNow,
            clientDeadline: null,
            capturedAt: capturedAt ?? DateTimeOffset.UtcNow,
            status: status);
    }

    [Fact]
    public async Task SearchAsync_EmptyDatabase_ReturnsEmpty()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityQueryRepository(context);
        var result = await repo.SearchAsync(new OpportunityQueryCriteria(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task SearchAsync_OneOpportunity_ReturnsOne()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var opp = CreateOpportunity(title: "Unique Title");
        await oppRepo.AddAsync(opp, CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(new OpportunityQueryCriteria(), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Unique Title", result.Items.First().Title);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_MultipleOpportunities_ReturnsAll()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        for (var i = 0; i < 5; i++)
        {
            await oppRepo.AddAsync(CreateOpportunity(title: $"Opp {i}"), CancellationToken.None);
        }

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(new OpportunityQueryCriteria { PageSize = 100 }, CancellationToken.None);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_Pagination_FirstPage()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        for (var i = 0; i < 10; i++)
        {
            await oppRepo.AddAsync(CreateOpportunity(title: $"Opp {i:D2}"), CancellationToken.None);
        }

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Page = 1, PageSize = 3 }, CancellationToken.None);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(10, result.TotalCount);
        Assert.Equal(4, result.TotalPages);
    }

    [Fact]
    public async Task SearchAsync_Pagination_SecondPage()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        for (var i = 0; i < 10; i++)
        {
            await oppRepo.AddAsync(CreateOpportunity(title: $"Opp {i:D2}"), CancellationToken.None);
        }

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Page = 2, PageSize = 3 }, CancellationToken.None);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task SearchAsync_PageSize1_ReturnsOne()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        for (var i = 0; i < 5; i++)
        {
            await oppRepo.AddAsync(CreateOpportunity(), CancellationToken.None);
        }

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { PageSize = 1 }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_MarketplaceFilter()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(marketplace: Marketplace.Workana), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(marketplace: Marketplace.Upwork), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(marketplace: Marketplace.Workana), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Marketplace = Marketplace.Workana, PageSize = 100 }, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i => Assert.Equal(Marketplace.Workana, i.Marketplace));
    }

    [Fact]
    public async Task SearchAsync_StatusFilter()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var opp1 = CreateOpportunity();
        var opp2 = CreateOpportunity();
        opp1.Shortlist();
        await oppRepo.AddAsync(opp1, CancellationToken.None);
        await oppRepo.AddAsync(opp2, CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Status = OpportunityStatus.Shortlisted, PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(OpportunityStatus.Shortlisted, result.Items.First().Status);
    }

    [Fact]
    public async Task SearchAsync_IntakeChannelFilter()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { IntakeChannel = IntakeChannel.Manual, PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_CapturedFromFilter()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var old = CreateOpportunity(capturedAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var recent = CreateOpportunity(capturedAt: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
        await oppRepo.AddAsync(old, CancellationToken.None);
        await oppRepo.AddAsync(recent, CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { CapturedFrom = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), PageSize = 100 },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero), result.Items.First().CapturedAt);
    }

    [Fact]
    public async Task SearchAsync_CapturedToFilter()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var old = CreateOpportunity(capturedAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var recent = CreateOpportunity(capturedAt: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
        await oppRepo.AddAsync(old, CancellationToken.None);
        await oppRepo.AddAsync(recent, CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { CapturedTo = new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.Zero), PageSize = 100 },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), result.Items.First().CapturedAt);
    }

    [Fact]
    public async Task SearchAsync_DateRange()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(capturedAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(capturedAt: new DateTimeOffset(2025, 3, 15, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(capturedAt: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(capturedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria
            {
                CapturedFrom = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                CapturedTo = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero),
                PageSize = 100
            },
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task SearchAsync_SearchTitle()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(title: "WhatsApp Integration"), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "React Dashboard"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "whatsapp", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("WhatsApp Integration", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_SearchDescription()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        var opp1 = Opportunity.Rehydrate(
            OpportunityId.New(), Marketplace.Workana, IntakeChannel.Manual, null,
            "Project A", "Build a chatbot using Python", null, null,
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            [], null, null, DateTimeOffset.UtcNow, OpportunityStatus.New);
        var opp2 = Opportunity.Rehydrate(
            OpportunityId.New(), Marketplace.Workana, IntakeChannel.Manual, null,
            "Project B", "Design a landing page", null, null,
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            [], null, null, DateTimeOffset.UtcNow, OpportunityStatus.New);

        await oppRepo.AddAsync(opp1, CancellationToken.None);
        await oppRepo.AddAsync(opp2, CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "chatbot", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("Project A", result.Items.First().Title);
    }

    [Fact]
    public async Task SearchAsync_SearchExternalReference()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(externalReference: "WK-12345"), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(externalReference: "UP-99999"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "WK-123", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("WK-12345", result.Items.First().ExternalReference);
    }

    [Fact]
    public async Task SearchAsync_SearchCategory()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(category: "Web Development"), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(category: "Mobile Development"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "web", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_SearchSubcategory()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(subcategory: "Frontend"), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(subcategory: "Backend"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "frontend", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_SearchClientDisplayName()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(clientDisplayName: "Acme Corp"), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(clientDisplayName: "Globex Inc"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "acme", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_SearchSkills()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(skills: ["C#", "ASP.NET"]), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(skills: ["Python", "Django"]), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "python", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_SearchCaseInsensitive()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(title: "WhatsApp Bot"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Search = "WHATSAPP", PageSize = 100 }, CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_SortCapturedAtDesc()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(title: "Old", capturedAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "New", capturedAt: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Sort = OpportunitySort.CapturedAtDesc, PageSize = 100 }, CancellationToken.None);

        Assert.Equal("New", result.Items.First().Title);
        Assert.Equal("Old", result.Items.Last().Title);
    }

    [Fact]
    public async Task SearchAsync_SortCapturedAtAsc()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(title: "Old", capturedAt: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "New", capturedAt: new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero)), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Sort = OpportunitySort.CapturedAtAsc, PageSize = 100 }, CancellationToken.None);

        Assert.Equal("Old", result.Items.First().Title);
        Assert.Equal("New", result.Items.Last().Title);
    }

    [Fact]
    public async Task SearchAsync_SortTitleAsc()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(title: "Zebra"), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "Alpha"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Sort = OpportunitySort.TitleAsc, PageSize = 100 }, CancellationToken.None);

        Assert.Equal("Alpha", result.Items.First().Title);
        Assert.Equal("Zebra", result.Items.Last().Title);
    }

    [Fact]
    public async Task SearchAsync_SortTitleDesc()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(title: "Alpha"), CancellationToken.None);
        await oppRepo.AddAsync(CreateOpportunity(title: "Zebra"), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(
            new OpportunityQueryCriteria { Sort = OpportunitySort.TitleDesc, PageSize = 100 }, CancellationToken.None);

        Assert.Equal("Zebra", result.Items.First().Title);
        Assert.Equal("Alpha", result.Items.Last().Title);
    }

    [Fact]
    public async Task SearchAsync_RequiredSkillsReturned()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var oppRepo = new EfOpportunityRepository(context);
        await oppRepo.AddAsync(CreateOpportunity(skills: ["C#", "React", "Flutter"]), CancellationToken.None);

        var queryRepo = new EfOpportunityQueryRepository(context);
        var result = await queryRepo.SearchAsync(new OpportunityQueryCriteria(), CancellationToken.None);

        var item = result.Items.First();
        Assert.Equal(3, item.RequiredSkills.Count);
        Assert.Contains("C#", item.RequiredSkills);
        Assert.Contains("React", item.RequiredSkills);
        Assert.Contains("Flutter", item.RequiredSkills);
    }
}

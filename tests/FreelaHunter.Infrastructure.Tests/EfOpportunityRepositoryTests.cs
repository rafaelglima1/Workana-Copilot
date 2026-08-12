using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;

namespace FreelaHunter.Infrastructure.Tests;

public class EfOpportunityRepositoryTests
{
    [Fact]
    public async Task AddAndGet_ReturnsOpportunity()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test Opportunity",
            description: "Description",
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL")),
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["C#", "API"]);

        await repository.AddAsync(opportunity, CancellationToken.None);

        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(opportunity.Id.Value, loaded!.Id.Value);
        Assert.Equal("Test Opportunity", loaded.Title);
    }

    [Fact]
    public async Task GetById_NotFound_ReturnsNull()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var result = await repository.GetByIdAsync(OpportunityId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddAndGet_RequiredSkillsRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["C#", "API", "Flutter"]);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded!.RequiredSkills.Count);
        Assert.Contains("C#", loaded.RequiredSkills);
        Assert.Contains("API", loaded.RequiredSkills);
        Assert.Contains("Flutter", loaded.RequiredSkills);
    }

    [Fact]
    public async Task AddAndGet_MarketplaceRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(Marketplace.Workana, loaded!.Marketplace);
    }

    [Fact]
    public async Task AddAndGet_OpportunityStatusRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        opportunity.Shortlist();

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(OpportunityStatus.Shortlisted, loaded!.Status);
    }

    [Fact]
    public async Task AddAndGet_ClientSnapshotRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(
                displayName: "Test Client",
                country: "BR",
                publishedProjects: 10,
                paidProjects: 8,
                rating: 4.5m),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("Test Client", loaded!.Client.DisplayName);
        Assert.Equal("BR", loaded.Client.Country);
        Assert.Equal(10, loaded.Client.PublishedProjects);
        Assert.Equal(8, loaded.Client.PaidProjects);
        Assert.Equal(4.5m, loaded.Client.Rating);
    }

    [Fact]
    public async Task AddAndGet_CompetitionSnapshotRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(proposalCount: 15, interestedFreelancerCount: 30),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(15, loaded!.Competition.ProposalCount);
        Assert.Equal(30, loaded.Competition.InterestedFreelancerCount);
    }

    [Fact]
    public async Task AddAndGet_BudgetRangeRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(BudgetKind.Range, loaded!.Budget.Kind);
        Assert.Equal(BudgetBasis.FixedProject, loaded.Budget.Basis);
        Assert.Equal(5000, loaded.Budget.Minimum);
        Assert.Equal(15000, loaded.Budget.Maximum);
        Assert.Equal("BRL", loaded.Budget.Currency!.Value);
    }

    [Fact]
    public async Task AddAndGet_BudgetExactRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Exact(10000, CurrencyCode.From("USD")),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(BudgetKind.Exact, loaded!.Budget.Kind);
        Assert.Equal(10000, loaded.Budget.Amount);
        Assert.Equal("USD", loaded.Budget.Currency!.Value);
    }

    [Fact]
    public async Task AddAndGet_BudgetUnknownRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(BudgetKind.Unknown, loaded!.Budget.Kind);
    }

    [Fact]
    public async Task AddAndGet_BudgetNegotiableRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Negotiable(CurrencyCode.From("BRL")),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(BudgetKind.Negotiable, loaded!.Budget.Kind);
        Assert.Equal("BRL", loaded.Budget.Currency!.Value);
    }

    [Fact]
    public async Task AddMultiple_OpportunitiesPersisted()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opp1 = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "First",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        var opp2 = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Second",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await repository.AddAsync(opp1, CancellationToken.None);
        await repository.AddAsync(opp2, CancellationToken.None);

        var loaded1 = await repository.GetByIdAsync(opp1.Id, CancellationToken.None);
        var loaded2 = await repository.GetByIdAsync(opp2.Id, CancellationToken.None);

        Assert.NotNull(loaded1);
        Assert.NotNull(loaded2);
        Assert.Equal("First", loaded1!.Title);
        Assert.Equal("Second", loaded2!.Title);
    }

    [Fact]
    public async Task AddAndGet_ExternalReferenceRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow,
            externalReference: "EXT-123");

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("EXT-123", loaded!.ExternalReference);
    }

    [Fact]
    public async Task AddAndGet_DatesRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfOpportunityRepository(context);

        var capturedAt = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var publishedAt = new DateTimeOffset(2025, 6, 16, 8, 0, 0, TimeSpan.Zero);
        var deadline = new DateOnly(2025, 7, 1);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: capturedAt,
            publishedAt: publishedAt,
            clientDeadline: deadline);

        await repository.AddAsync(opportunity, CancellationToken.None);
        var loaded = await repository.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(capturedAt, loaded!.CapturedAt);
        Assert.Equal(publishedAt, loaded.PublishedAt);
        Assert.Equal(deadline, loaded.ClientDeadline);
    }
}

using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;

namespace FreelaHunter.Infrastructure.Tests;

public class EfOpportunityRepositoryUpdateTests
{
    [Fact]
    public async Task UpdateAsync_ShortlistedPersists()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Shortlist();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(OpportunityStatus.Shortlisted, loaded!.Status);
    }

    [Fact]
    public async Task UpdateAsync_IgnoredPersists()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Ignore();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(OpportunityStatus.Ignored, loaded!.Status);
    }

    [Fact]
    public async Task UpdateAsync_ArchivedPersists()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Archive();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(OpportunityStatus.Archived, loaded!.Status);
    }

    [Fact]
    public async Task UpdateAsync_RequiredSkillsNotLost()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow, requiredSkills: ["C#", "React"]);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Shortlist();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.RequiredSkills.Count);
        Assert.Contains("C#", loaded.RequiredSkills);
        Assert.Contains("React", loaded.RequiredSkills);
    }

    [Fact]
    public async Task UpdateAsync_BudgetNotChanged()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Range(5000, 15000, CurrencyCode.From("BRL")),
            ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Shortlist();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(BudgetKind.Range, loaded!.Budget.Kind);
        Assert.Equal(5000, loaded.Budget.Minimum);
        Assert.Equal(15000, loaded.Budget.Maximum);
        Assert.Equal("BRL", loaded.Budget.Currency!.Value);
    }

    [Fact]
    public async Task UpdateAsync_ClientSnapshotNotChanged()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(),
            ClientSnapshot.Create(displayName: "Test Client", country: "BR", publishedProjects: 10, paidProjects: 8, rating: 4.5m),
            CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Shortlist();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal("Test Client", loaded!.Client.DisplayName);
        Assert.Equal("BR", loaded.Client.Country);
        Assert.Equal(10, loaded.Client.PublishedProjects);
    }

    [Fact]
    public async Task UpdateAsync_CompetitionNotChanged()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(),
            CompetitionSnapshot.Create(proposalCount: 15, interestedFreelancerCount: 30),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Shortlist();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Equal(15, loaded!.Competition.ProposalCount);
        Assert.Equal(30, loaded.Competition.InterestedFreelancerCount);
    }

    [Fact]
    public async Task UpdateAsync_NoDuplicateSkills()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repo = new EfOpportunityRepository(context);
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow, requiredSkills: ["C#"]);
        await repo.AddAsync(opp, CancellationToken.None);

        opp.Shortlist();
        await repo.UpdateAsync(opp, CancellationToken.None);
        opp.Ignore();
        await repo.UpdateAsync(opp, CancellationToken.None);

        var loaded = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(loaded);
        Assert.Single(loaded!.RequiredSkills);
    }
}

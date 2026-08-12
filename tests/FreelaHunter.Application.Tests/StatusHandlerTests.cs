using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class ShortlistOpportunityHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingOpportunity_ReturnsShortlistedResponse()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new ShortlistOpportunityHandler(repo);
        var result = await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ContractOpportunityStatus.Shortlisted, result!.Status);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNull()
    {
        var repo = new FakeOpportunityRepository();
        var handler = new ShortlistOpportunityHandler(repo);

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_Idempotent_ShortlistOnShortlisted()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        opp.Shortlist();
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new ShortlistOpportunityHandler(repo);
        var result = await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ContractOpportunityStatus.Shortlisted, result!.Status);
    }

    [Fact]
    public async Task HandleAsync_CallsUpdateAsync()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new ShortlistOpportunityHandler(repo);
        await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.Single(repo.UpdatedOpportunities);
    }
}

public class IgnoreOpportunityHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingOpportunity_ReturnsIgnoredResponse()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new IgnoreOpportunityHandler(repo);
        var result = await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ContractOpportunityStatus.Ignored, result!.Status);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNull()
    {
        var repo = new FakeOpportunityRepository();
        var handler = new IgnoreOpportunityHandler(repo);

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_Idempotent_IgnoreOnIgnored()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        opp.Ignore();
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new IgnoreOpportunityHandler(repo);
        var result = await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ContractOpportunityStatus.Ignored, result!.Status);
    }

    [Fact]
    public async Task HandleAsync_CallsUpdateAsync()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new IgnoreOpportunityHandler(repo);
        await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.Single(repo.UpdatedOpportunities);
    }
}

public class ArchiveOpportunityHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingOpportunity_ReturnsArchivedResponse()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new ArchiveOpportunityHandler(repo);
        var result = await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ContractOpportunityStatus.Archived, result!.Status);
    }

    [Fact]
    public async Task HandleAsync_NotFound_ReturnsNull()
    {
        var repo = new FakeOpportunityRepository();
        var handler = new ArchiveOpportunityHandler(repo);

        var result = await handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_Idempotent_ArchiveOnArchived()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        opp.Archive();
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new ArchiveOpportunityHandler(repo);
        var result = await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(ContractOpportunityStatus.Archived, result!.Status);
    }

    [Fact]
    public async Task HandleAsync_CallsUpdateAsync()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new ArchiveOpportunityHandler(repo);
        await handler.HandleAsync(opp.Id.Value, CancellationToken.None);

        Assert.Single(repo.UpdatedOpportunities);
    }

    [Fact]
    public async Task HandleAsync_ArchivedToShortlist_ThrowsDomainException()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        opp.Archive();
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new ShortlistOpportunityHandler(repo);
        await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            handler.HandleAsync(opp.Id.Value, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ArchivedToIgnore_ThrowsDomainException()
    {
        var repo = new FakeOpportunityRepository();
        var opp = Opportunity.Create(
            Marketplace.Workana, IntakeChannel.Manual, "Test", "Description",
            Budget.Unknown(), ClientSnapshot.Create(), CompetitionSnapshot.Create(),
            DateTimeOffset.UtcNow);
        opp.Archive();
        await repo.AddAsync(opp, CancellationToken.None);

        var handler = new IgnoreOpportunityHandler(repo);
        await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            handler.HandleAsync(opp.Id.Value, CancellationToken.None));
    }
}

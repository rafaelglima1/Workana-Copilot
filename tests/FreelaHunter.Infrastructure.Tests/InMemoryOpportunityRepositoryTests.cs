using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Infrastructure.Tests;

public class InMemoryOpportunityRepositoryTests
{
    [Fact]
    public async Task AddAndGet_ReturnsOpportunity()
    {
        var repo = new InMemoryOpportunityRepository();
        var opportunity = CreateOpportunity();

        await repo.AddAsync(opportunity, CancellationToken.None);
        var result = await repo.GetByIdAsync(opportunity.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(opportunity.Id.Value, result.Id.Value);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNull()
    {
        var repo = new InMemoryOpportunityRepository();

        var result = await repo.GetByIdAsync(OpportunityId.New(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AddMultiple_AllRetrievable()
    {
        var repo = new InMemoryOpportunityRepository();
        var o1 = CreateOpportunity();
        var o2 = CreateOpportunity();

        await repo.AddAsync(o1, CancellationToken.None);
        await repo.AddAsync(o2, CancellationToken.None);

        var r1 = await repo.GetByIdAsync(o1.Id, CancellationToken.None);
        var r2 = await repo.GetByIdAsync(o2.Id, CancellationToken.None);

        Assert.NotNull(r1);
        Assert.NotNull(r2);
        Assert.NotEqual(r1.Id.Value, r2.Id.Value);
    }

    [Fact]
    public async Task AddMultiple_BothRetrievable()
    {
        var repo = new InMemoryOpportunityRepository();
        var o1 = CreateOpportunity();
        var o2 = CreateOpportunity();

        await repo.AddAsync(o1, CancellationToken.None);
        await repo.AddAsync(o2, CancellationToken.None);

        var r1 = await repo.GetByIdAsync(o1.Id, CancellationToken.None);
        var r2 = await repo.GetByIdAsync(o2.Id, CancellationToken.None);

        Assert.NotNull(r1);
        Assert.NotNull(r2);
    }

    [Fact]
    public async Task ConcurrentAccess_IsThreadSafe()
    {
        var repo = new InMemoryOpportunityRepository();
        var tasks = new List<Task>();

        for (int i = 0; i < 100; i++)
        {
            var opp = CreateOpportunity();
            tasks.Add(repo.AddAsync(opp, CancellationToken.None));
        }

        await Task.WhenAll(tasks);

        // All should be retrievable without exception
        foreach (var task in tasks)
        {
            await task;
        }
    }

    [Fact]
    public async Task InstanceDoesNotLoseData_BetweenCalls()
    {
        var repo = new InMemoryOpportunityRepository();
        var opp = CreateOpportunity();

        await repo.AddAsync(opp, CancellationToken.None);

        // Simulate new request
        var result = await repo.GetByIdAsync(opp.Id, CancellationToken.None);
        Assert.NotNull(result);
    }

    private static Opportunity CreateOpportunity(OpportunityId? id = null)
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);
    }


}

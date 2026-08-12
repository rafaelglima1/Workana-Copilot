using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Tests;

public class RestartPersistenceTests
{
    [Fact]
    public async Task Opportunity_SurvivesContextRestart()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Persistent Opportunity",
            description: "Description",
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL")),
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["C#", "API"]);

        using (var context1 = new FreelaHunterDbContext(
            new DbContextOptionsBuilder<FreelaHunterDbContext>()
                .UseSqlite(connection)
                .Options))
        {
            await context1.Database.EnsureCreatedAsync();
            var repo1 = new EfOpportunityRepository(context1);
            await repo1.AddAsync(opportunity, CancellationToken.None);
        }

        using (var context2 = new FreelaHunterDbContext(
            new DbContextOptionsBuilder<FreelaHunterDbContext>()
                .UseSqlite(connection)
                .Options))
        {
            var repo2 = new EfOpportunityRepository(context2);
            var loaded = await repo2.GetByIdAsync(opportunity.Id, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal("Persistent Opportunity", loaded!.Title);
            Assert.Equal(2, loaded.RequiredSkills.Count);
            Assert.Contains("C#", loaded.RequiredSkills);
            Assert.Contains("API", loaded.RequiredSkills);
            Assert.Equal(Marketplace.Workana, loaded.Marketplace);
            Assert.Equal(5000, loaded.Budget.Minimum);
            Assert.Equal(15000, loaded.Budget.Maximum);
        }

        await connection.CloseAsync();
    }

    [Fact]
    public async Task FreelancerProfile_SurvivesContextRestart()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var profile = FreelancerProfile.Create(
            [
                SkillCapability.Create("C#", SkillLevel.Expert),
                SkillCapability.Create("React", SkillLevel.Strong)
            ],
            [
                HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
                HourlyRateTarget.Create(65, CurrencyCode.From("USD"))
            ],
            CurrencyCode.From("BRL"));

        using (var context1 = new FreelaHunterDbContext(
            new DbContextOptionsBuilder<FreelaHunterDbContext>()
                .UseSqlite(connection)
                .Options))
        {
            await context1.Database.EnsureCreatedAsync();
            var repo1 = new EfFreelancerProfileRepository(context1);
            await repo1.SaveAsync(profile, CancellationToken.None);
        }

        using (var context2 = new FreelaHunterDbContext(
            new DbContextOptionsBuilder<FreelaHunterDbContext>()
                .UseSqlite(connection)
                .Options))
        {
            var repo2 = new EfFreelancerProfileRepository(context2);
            var loaded = await repo2.GetCurrentAsync(CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal(profile.Id.Value, loaded!.Id.Value);
            Assert.Equal(2, loaded.Skills.Count);
            Assert.Equal(2, loaded.HourlyRateTargets.Count);
            Assert.Equal("BRL", loaded.DefaultRateCurrency.Value);
        }

        await connection.CloseAsync();
    }
}

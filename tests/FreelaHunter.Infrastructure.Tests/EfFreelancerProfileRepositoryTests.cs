using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence;
using FreelaHunter.Infrastructure.Persistence.Repositories;

namespace FreelaHunter.Infrastructure.Tests;

public class EfFreelancerProfileRepositoryTests
{
    [Fact]
    public async Task SaveAndGet_ReturnsProfile()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var profile = FreelancerProfile.Create(
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))],
            CurrencyCode.From("BRL"));

        await repository.SaveAsync(profile, CancellationToken.None);

        var loaded = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(profile.Id.Value, loaded!.Id.Value);
    }

    [Fact]
    public async Task GetCurrent_NoProfile_ReturnsNull()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var result = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndGet_SkillsRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var profile = FreelancerProfile.Create(
            [
                SkillCapability.Create("C#", SkillLevel.Expert),
                SkillCapability.Create("React", SkillLevel.Strong),
                SkillCapability.Create("PostgreSQL", SkillLevel.Working)
            ],
            [HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))],
            CurrencyCode.From("BRL"));

        await repository.SaveAsync(profile, CancellationToken.None);
        var loaded = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(3, loaded!.Skills.Count);
        Assert.Contains(loaded.Skills, s => s.Name == "C#" && s.Level == SkillLevel.Expert);
        Assert.Contains(loaded.Skills, s => s.Name == "React" && s.Level == SkillLevel.Strong);
        Assert.Contains(loaded.Skills, s => s.Name == "PostgreSQL" && s.Level == SkillLevel.Working);
    }

    [Fact]
    public async Task SaveAndGet_RatesRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var profile = FreelancerProfile.Create(
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [
                HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
                HourlyRateTarget.Create(65, CurrencyCode.From("USD"))
            ],
            CurrencyCode.From("BRL"));

        await repository.SaveAsync(profile, CancellationToken.None);
        var loaded = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.HourlyRateTargets.Count);
        Assert.Contains(loaded.HourlyRateTargets, r => r.Amount == 100 && r.Currency.Value == "BRL");
        Assert.Contains(loaded.HourlyRateTargets, r => r.Amount == 65 && r.Currency.Value == "USD");
    }

    [Fact]
    public async Task SaveAndGet_DefaultCurrencyRoundtrip()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var profile = FreelancerProfile.Create(
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [HourlyRateTarget.Create(65, CurrencyCode.From("USD"))],
            CurrencyCode.From("USD"));

        await repository.SaveAsync(profile, CancellationToken.None);
        var loaded = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("USD", loaded!.DefaultRateCurrency.Value);
    }

    [Fact]
    public async Task SaveTwice_UpdatesProfile()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var profile1 = FreelancerProfile.Create(
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))],
            CurrencyCode.From("BRL"));

        await repository.SaveAsync(profile1, CancellationToken.None);

        var profile2 = FreelancerProfile.Rehydrate(
            profile1.Id,
            [SkillCapability.Create("React", SkillLevel.Strong)],
            [HourlyRateTarget.Create(65, CurrencyCode.From("USD"))],
            CurrencyCode.From("USD"));

        await repository.SaveAsync(profile2, CancellationToken.None);

        var loaded = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(profile1.Id.Value, loaded!.Id.Value);
        Assert.Single(loaded.Skills);
        Assert.Equal("React", loaded.Skills.First().Name);
        Assert.Single(loaded.HourlyRateTargets);
        Assert.Equal("USD", loaded.HourlyRateTargets.First().Currency.Value);
    }

    [Fact]
    public async Task SaveTwice_RemovesOldSkills()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var profile1 = FreelancerProfile.Create(
            [
                SkillCapability.Create("C#", SkillLevel.Expert),
                SkillCapability.Create("React", SkillLevel.Strong)
            ],
            [HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))],
            CurrencyCode.From("BRL"));

        await repository.SaveAsync(profile1, CancellationToken.None);

        var profile2 = FreelancerProfile.Rehydrate(
            profile1.Id,
            [SkillCapability.Create("Flutter", SkillLevel.Working)],
            [HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))],
            CurrencyCode.From("BRL"));

        await repository.SaveAsync(profile2, CancellationToken.None);

        var loaded = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.Skills);
        Assert.Equal("Flutter", loaded.Skills.First().Name);
    }

    [Fact]
    public async Task SaveTwice_RemovesOldRates()
    {
        var (context, connection) = DbContextHelper.CreateInMemoryContext();
        await using var _ = connection;
        await using var __ = context;

        var repository = new EfFreelancerProfileRepository(context);

        var profile1 = FreelancerProfile.Create(
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [
                HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
                HourlyRateTarget.Create(65, CurrencyCode.From("USD"))
            ],
            CurrencyCode.From("BRL"));

        await repository.SaveAsync(profile1, CancellationToken.None);

        var profile2 = FreelancerProfile.Rehydrate(
            profile1.Id,
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [HourlyRateTarget.Create(50, CurrencyCode.From("EUR"))],
            CurrencyCode.From("EUR"));

        await repository.SaveAsync(profile2, CancellationToken.None);

        var loaded = await repository.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Single(loaded!.HourlyRateTargets);
        Assert.Equal("EUR", loaded.HourlyRateTargets.First().Currency.Value);
    }
}

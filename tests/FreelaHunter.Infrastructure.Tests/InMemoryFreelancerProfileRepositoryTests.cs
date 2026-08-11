using FreelaHunter.Infrastructure;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Infrastructure.Tests;

public class InMemoryFreelancerProfileRepositoryTests
{
    [Fact]
    public async Task GetCurrent_NoProfile_ReturnsNull()
    {
        var repo = new InMemoryFreelancerProfileRepository();

        var result = await repo.GetCurrentAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndGet_ReturnsSavedProfile()
    {
        var repo = new InMemoryFreelancerProfileRepository();
        var profile = CreateProfile();

        await repo.SaveAsync(profile, CancellationToken.None);
        var result = await repo.GetCurrentAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(profile.Id.Value, result!.Id.Value);
    }

    [Fact]
    public async Task SecondSave_ReplacesCurrentProfile()
    {
        var repo = new InMemoryFreelancerProfileRepository();
        var profile1 = CreateProfile();
        var profile2 = FreelancerProfile.Create(
            [SkillCapability.Create("Flutter", SkillLevel.Expert)],
            [HourlyRateTarget.Create(50, CurrencyCode.From("USD"))],
            CurrencyCode.From("USD"));

        await repo.SaveAsync(profile1, CancellationToken.None);
        await repo.SaveAsync(profile2, CancellationToken.None);
        var result = await repo.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(profile2.Id.Value, result!.Id.Value);
    }

    [Fact]
    public async Task IdPreserved_WhenSameProfileUpdated()
    {
        var repo = new InMemoryFreelancerProfileRepository();
        var profile = CreateProfile();
        var originalId = profile.Id.Value;

        await repo.SaveAsync(profile, CancellationToken.None);

        profile.Update(
            [SkillCapability.Create("C#", SkillLevel.Expert), SkillCapability.Create("React", SkillLevel.Strong)],
            [HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))],
            CurrencyCode.From("BRL"));

        await repo.SaveAsync(profile, CancellationToken.None);
        var result = await repo.GetCurrentAsync(CancellationToken.None);

        Assert.Equal(originalId, result!.Id.Value);
    }

    [Fact]
    public async Task ConcurrentAccess_IsThreadSafe()
    {
        var repo = new InMemoryFreelancerProfileRepository();
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(async () =>
            {
                var profile = CreateProfile();
                await repo.SaveAsync(profile, CancellationToken.None);
                await repo.GetCurrentAsync(CancellationToken.None);
            }));

        await Task.WhenAll(tasks);

        var result = await repo.GetCurrentAsync(CancellationToken.None);
        Assert.NotNull(result);
    }

    private static FreelancerProfile CreateProfile()
    {
        return FreelancerProfile.Create(
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))],
            CurrencyCode.From("BRL"));
    }
}

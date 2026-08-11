using FreelaHunter.Application;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

internal sealed class FakeFreelancerProfileRepository : IFreelancerProfileRepository
{
    private FreelancerProfile? _current;

    public FreelancerProfile? SavedProfile { get; private set; }
    public int SaveCount { get; private set; }

    public Task<FreelancerProfile?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_current);
    }

    public Task SaveAsync(FreelancerProfile profile, CancellationToken cancellationToken)
    {
        _current = profile;
        SavedProfile = profile;
        SaveCount++;
        return Task.CompletedTask;
    }

    public static FreelancerProfile CreateDefaultProfile()
    {
        return FreelancerProfile.Create(
            [
                SkillCapability.Create("C#", SkillLevel.Expert),
                SkillCapability.Create("API", SkillLevel.Expert),
                SkillCapability.Create("Flutter", SkillLevel.None)
            ],
            [
                HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
                HourlyRateTarget.Create(65, CurrencyCode.From("USD"))
            ],
            CurrencyCode.From("BRL"));
    }
}

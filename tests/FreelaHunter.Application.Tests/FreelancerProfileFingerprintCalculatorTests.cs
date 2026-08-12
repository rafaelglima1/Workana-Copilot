using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class FreelancerProfileFingerprintCalculatorTests
{
    [Fact]
    public void SameProfile_SameFingerprint()
    {
        var profile = CreateProfile();

        var fp1 = FreelancerProfileFingerprintCalculator.Calculate(profile);
        var fp2 = FreelancerProfileFingerprintCalculator.Calculate(profile);

        Assert.Equal(fp1, fp2);
    }

    [Fact]
    public void DifferentSkills_DifferentFingerprint()
    {
        var profile1 = CreateProfile(skills: [
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("API", SkillLevel.Expert)
        ]);
        var profile2 = CreateProfile(skills: [
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("Docker", SkillLevel.Working)
        ]);

        var fp1 = FreelancerProfileFingerprintCalculator.Calculate(profile1);
        var fp2 = FreelancerProfileFingerprintCalculator.Calculate(profile2);

        Assert.NotEqual(fp1, fp2);
    }

    [Fact]
    public void DifferentRates_DifferentFingerprint()
    {
        var profile1 = CreateProfile(rates: [
            HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
            HourlyRateTarget.Create(65, CurrencyCode.From("USD"))
        ], defaultCurrency: CurrencyCode.From("BRL"));
        var profile2 = CreateProfile(rates: [
            HourlyRateTarget.Create(120, CurrencyCode.From("BRL")),
            HourlyRateTarget.Create(75, CurrencyCode.From("USD"))
        ], defaultCurrency: CurrencyCode.From("BRL"));

        var fp1 = FreelancerProfileFingerprintCalculator.Calculate(profile1);
        var fp2 = FreelancerProfileFingerprintCalculator.Calculate(profile2);

        Assert.NotEqual(fp1, fp2);
    }

    [Fact]
    public void DifferentDefaultCurrency_DifferentFingerprint()
    {
        var profile1 = CreateProfile(defaultCurrency: CurrencyCode.From("BRL"));
        var profile2 = CreateProfile(defaultCurrency: CurrencyCode.From("USD"));

        var fp1 = FreelancerProfileFingerprintCalculator.Calculate(profile1);
        var fp2 = FreelancerProfileFingerprintCalculator.Calculate(profile2);

        Assert.NotEqual(fp1, fp2);
    }

    [Fact]
    public void SkillOrderDoesNotMatter()
    {
        var profile1 = CreateProfile(skills: [
            SkillCapability.Create("C#", SkillLevel.Expert),
            SkillCapability.Create("API", SkillLevel.Expert)
        ]);
        var profile2 = CreateProfile(skills: [
            SkillCapability.Create("API", SkillLevel.Expert),
            SkillCapability.Create("C#", SkillLevel.Expert)
        ]);

        var fp1 = FreelancerProfileFingerprintCalculator.Calculate(profile1);
        var fp2 = FreelancerProfileFingerprintCalculator.Calculate(profile2);

        Assert.Equal(fp1, fp2);
    }

    [Fact]
    public void RateOrderDoesNotMatter()
    {
        var profile1 = CreateProfile(rates: [
            HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
            HourlyRateTarget.Create(65, CurrencyCode.From("USD"))
        ], defaultCurrency: CurrencyCode.From("BRL"));
        var profile2 = CreateProfile(rates: [
            HourlyRateTarget.Create(65, CurrencyCode.From("USD")),
            HourlyRateTarget.Create(100, CurrencyCode.From("BRL"))
        ], defaultCurrency: CurrencyCode.From("BRL"));

        var fp1 = FreelancerProfileFingerprintCalculator.Calculate(profile1);
        var fp2 = FreelancerProfileFingerprintCalculator.Calculate(profile2);

        Assert.Equal(fp1, fp2);
    }

    [Fact]
    public void ReturnsSha256Hex()
    {
        var profile = CreateProfile();

        var fp = FreelancerProfileFingerprintCalculator.Calculate(profile);

        Assert.Equal(64, fp.Length);
        Assert.Matches("^[a-f0-9]{64}$", fp);
    }

    private static FreelancerProfile CreateProfile(
        IEnumerable<SkillCapability>? skills = null,
        IEnumerable<HourlyRateTarget>? rates = null,
        CurrencyCode? defaultCurrency = null)
    {
        return FreelancerProfile.Rehydrate(
            FreelancerProfileId.From(Guid.NewGuid()),
            skills ?? [
                SkillCapability.Create("C#", SkillLevel.Expert),
                SkillCapability.Create("API", SkillLevel.Expert),
                SkillCapability.Create("Flutter", SkillLevel.None)
            ],
            rates ?? [
                HourlyRateTarget.Create(100, CurrencyCode.From("BRL")),
                HourlyRateTarget.Create(65, CurrencyCode.From("USD"))
            ],
            defaultCurrency ?? CurrencyCode.From("BRL"));
    }
}

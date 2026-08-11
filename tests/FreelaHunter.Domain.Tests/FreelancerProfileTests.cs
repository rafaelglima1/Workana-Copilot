using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class FreelancerProfileTests
{
    private static SkillCapability Skill(string name, SkillLevel level = SkillLevel.Strong) =>
        SkillCapability.Create(name, level);

    private static HourlyRateTarget Rate(decimal amount, string currency) =>
        HourlyRateTarget.Create(amount, CurrencyCode.From(currency));

    [Fact]
    public void Create_ValidProfile_Succeeds()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#", SkillLevel.Expert)],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.NotEqual(Guid.Empty, profile.Id.Value);
        Assert.Single(profile.Skills);
        Assert.Single(profile.HourlyRateTargets);
        Assert.Equal("BRL", profile.DefaultRateCurrency.Value);
    }

    [Fact]
    public void Create_MultipleSkills_Succeeds()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#", SkillLevel.Expert), Skill("Flutter", SkillLevel.Working)],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.Equal(2, profile.Skills.Count);
    }

    [Fact]
    public void Create_DuplicateSkills_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => FreelancerProfile.Create(
            [Skill("C#", SkillLevel.Expert), Skill("c#", SkillLevel.Basic)],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Create_NoSkills_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => FreelancerProfile.Create(
            [],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Create_NullSkills_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => FreelancerProfile.Create(
            null!,
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Create_MultipleRates_Succeeds()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL"), Rate(65, "USD")],
            CurrencyCode.From("BRL"));

        Assert.Equal(2, profile.HourlyRateTargets.Count);
    }

    [Fact]
    public void Create_DuplicateCurrencyRates_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL"), Rate(200, "BRL")],
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Create_NoRates_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => FreelancerProfile.Create(
            [Skill("C#")],
            [],
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Create_NullRates_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => FreelancerProfile.Create(
            [Skill("C#")],
            null!,
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Create_DefaultCurrencyExists_Succeeds()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL"), Rate(65, "USD")],
            CurrencyCode.From("USD"));

        Assert.Equal("USD", profile.DefaultRateCurrency.Value);
    }

    [Fact]
    public void Create_DefaultCurrencyNotInRates_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("USD")));
    }

    [Fact]
    public void SkillsCollection_IsImmutable()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.IsAssignableFrom<IReadOnlyCollection<SkillCapability>>(profile.Skills);
    }

    [Fact]
    public void RatesCollection_IsImmutable()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.IsAssignableFrom<IReadOnlyCollection<HourlyRateTarget>>(profile.HourlyRateTargets);
    }

    [Fact]
    public void Update_Skills_Succeeds()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));
        var originalId = profile.Id;

        profile.Update(
            [Skill("C#", SkillLevel.Expert), Skill("Flutter", SkillLevel.Working)],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.Equal(originalId.Value, profile.Id.Value);
        Assert.Equal(2, profile.Skills.Count);
    }

    [Fact]
    public void Update_Rates_Succeeds()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        profile.Update(
            [Skill("C#")],
            [Rate(100, "BRL"), Rate(65, "USD")],
            CurrencyCode.From("BRL"));

        Assert.Equal(2, profile.HourlyRateTargets.Count);
    }

    [Fact]
    public void Update_PreservesId()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));
        var originalId = profile.Id.Value;

        profile.Update(
            [Skill("Flutter")],
            [Rate(50, "USD")],
            CurrencyCode.From("USD"));

        Assert.Equal(originalId, profile.Id.Value);
    }

    [Fact]
    public void Update_ZeroSkills_ThrowsDomainException()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.Throws<DomainException>(() => profile.Update(
            [],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Update_ZeroRates_ThrowsDomainException()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.Throws<DomainException>(() => profile.Update(
            [Skill("C#")],
            [],
            CurrencyCode.From("BRL")));
    }

    [Fact]
    public void Update_DefaultCurrencyNotInRates_ThrowsDomainException()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("BRL"));

        Assert.Throws<DomainException>(() => profile.Update(
            [Skill("C#")],
            [Rate(100, "BRL")],
            CurrencyCode.From("USD")));
    }

    [Fact]
    public void ResolveHourlyRate_PreferredCurrencyExists_ReturnsPreferred()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL"), Rate(65, "USD")],
            CurrencyCode.From("BRL"));

        var rate = profile.ResolveHourlyRate(CurrencyCode.From("USD"));

        Assert.Equal(65, rate.Amount);
        Assert.Equal("USD", rate.Currency.Value);
    }

    [Fact]
    public void ResolveHourlyRate_PreferredCurrencyNotExists_ReturnsDefault()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL"), Rate(65, "USD")],
            CurrencyCode.From("BRL"));

        var rate = profile.ResolveHourlyRate(CurrencyCode.From("EUR"));

        Assert.Equal(100, rate.Amount);
        Assert.Equal("BRL", rate.Currency.Value);
    }

    [Fact]
    public void ResolveHourlyRate_Null_ReturnsDefault()
    {
        var profile = FreelancerProfile.Create(
            [Skill("C#")],
            [Rate(100, "BRL"), Rate(65, "USD")],
            CurrencyCode.From("BRL"));

        var rate = profile.ResolveHourlyRate(null);

        Assert.Equal(100, rate.Amount);
        Assert.Equal("BRL", rate.Currency.Value);
    }
}

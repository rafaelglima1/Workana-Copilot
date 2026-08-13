using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence.Models;

namespace FreelaHunter.Infrastructure.Persistence.Mappers;

public static class FreelancerProfilePersistenceMapper
{
    public static FreelancerProfileRecord ToRecord(FreelancerProfile profile)
    {
        return new FreelancerProfileRecord
        {
            Id = profile.Id.Value,
            DefaultRateCurrency = profile.DefaultRateCurrency.Value,
            WeeklyCapacityHours = profile.WeeklyCapacityHours,
            Skills = profile.Skills.Select(s => new FreelancerProfileSkillRecord
            {
                FreelancerProfileId = profile.Id.Value,
                Name = s.Name,
                Level = s.Level.ToString()
            }).ToList(),
            HourlyRates = profile.HourlyRateTargets.Select(r => new FreelancerHourlyRateRecord
            {
                FreelancerProfileId = profile.Id.Value,
                Amount = r.Amount,
                Currency = r.Currency.Value
            }).ToList()
        };
    }

    public static FreelancerProfile ToDomain(FreelancerProfileRecord record)
    {
        var id = FreelancerProfileId.From(record.Id);
        var skills = record.Skills.Select(s =>
            SkillCapability.Create(s.Name, Enum.Parse<SkillLevel>(s.Level))).ToList();
        var rates = record.HourlyRates.Select(r =>
            HourlyRateTarget.Create(r.Amount, CurrencyCode.From(r.Currency))).ToList();
        var defaultCurrency = CurrencyCode.From(record.DefaultRateCurrency);

        return FreelancerProfile.Rehydrate(id, skills, rates, defaultCurrency, record.WeeklyCapacityHours);
    }
}

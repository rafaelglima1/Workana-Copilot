using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Entities;

public sealed class FreelancerProfile
{
    private readonly List<SkillCapability> _skills = [];
    private readonly List<HourlyRateTarget> _hourlyRateTargets = [];

    public FreelancerProfileId Id { get; }
    public IReadOnlyCollection<SkillCapability> Skills => _skills.AsReadOnly();
    public IReadOnlyCollection<HourlyRateTarget> HourlyRateTargets => _hourlyRateTargets.AsReadOnly();
    public CurrencyCode DefaultRateCurrency { get; private set; }
    public decimal? WeeklyCapacityHours { get; private set; }

    private FreelancerProfile(
        FreelancerProfileId id,
        IReadOnlyCollection<SkillCapability> skills,
        IReadOnlyCollection<HourlyRateTarget> hourlyRateTargets,
        CurrencyCode defaultRateCurrency,
        decimal? weeklyCapacityHours)
    {
        Id = id;
        _skills.AddRange(skills);
        _hourlyRateTargets.AddRange(hourlyRateTargets);
        DefaultRateCurrency = defaultRateCurrency;
        WeeklyCapacityHours = weeklyCapacityHours;
    }

    public static FreelancerProfile Create(
        IEnumerable<SkillCapability> skills,
        IEnumerable<HourlyRateTarget> hourlyRateTargets,
        CurrencyCode defaultRateCurrency,
        decimal? weeklyCapacityHours = null)
    {
        var distinctSkills = ValidateSkills(skills);
        var distinctRates = ValidateRates(hourlyRateTargets);
        ValidateDefaultCurrency(defaultRateCurrency, distinctRates);
        ValidateWeeklyCapacity(weeklyCapacityHours);

        return new FreelancerProfile(
            FreelancerProfileId.New(),
            distinctSkills,
            distinctRates,
            defaultRateCurrency,
            weeklyCapacityHours);
    }

    public void Update(
        IEnumerable<SkillCapability> skills,
        IEnumerable<HourlyRateTarget> hourlyRateTargets,
        CurrencyCode defaultRateCurrency,
        decimal? weeklyCapacityHours = null)
    {
        var distinctSkills = ValidateSkills(skills);
        var distinctRates = ValidateRates(hourlyRateTargets);
        ValidateDefaultCurrency(defaultRateCurrency, distinctRates);
        ValidateWeeklyCapacity(weeklyCapacityHours);

        _skills.Clear();
        _skills.AddRange(distinctSkills);

        _hourlyRateTargets.Clear();
        _hourlyRateTargets.AddRange(distinctRates);

        DefaultRateCurrency = defaultRateCurrency;
        WeeklyCapacityHours = weeklyCapacityHours;
    }

    public HourlyRateTarget ResolveHourlyRate(CurrencyCode? preferredCurrency)
    {
        if (preferredCurrency is not null)
        {
            var match = _hourlyRateTargets
                .FirstOrDefault(r => r.Currency.Value == preferredCurrency.Value);

            if (match is not null)
                return match;
        }

        return _hourlyRateTargets.First(r => r.Currency.Value == DefaultRateCurrency.Value);
    }

    private static IReadOnlyCollection<SkillCapability> ValidateSkills(IEnumerable<SkillCapability> skills)
    {
        if (skills is null)
            throw new DomainException("Skills collection is required.");

        var list = skills.ToList();
        if (list.Count == 0)
            throw new DomainException("At least one skill is required.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var canonicalSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var skill in list)
        {
            if (skill is null)
                throw new DomainException("Skill cannot be null.");

            if (!seen.Add(skill.Name))
                throw new DomainException($"Duplicate skill '{skill.Name}' is not allowed.");

            var canonical = SkillAliasCatalog.Default.ResolveCanonical(skill.Name);
            if (canonical is not null)
            {
                if (!canonicalSeen.Add(canonical))
                    throw new DomainException(
                        $"Skill '{skill.Name}' is semantically equivalent to another skill via alias catalog.");
            }
        }

        return list.AsReadOnly();
    }

    private static IReadOnlyCollection<HourlyRateTarget> ValidateRates(IEnumerable<HourlyRateTarget> rates)
    {
        if (rates is null)
            throw new DomainException("HourlyRateTargets collection is required.");

        var list = rates.ToList();
        if (list.Count == 0)
            throw new DomainException("At least one hourly rate target is required.");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rate in list)
        {
            if (rate is null)
                throw new DomainException("Hourly rate cannot be null.");

            if (!seen.Add(rate.Currency.Value))
                throw new DomainException($"Duplicate currency '{rate.Currency.Value}' is not allowed in hourly rate targets.");
        }

        return list.AsReadOnly();
    }

    private static void ValidateDefaultCurrency(CurrencyCode defaultCurrency, IReadOnlyCollection<HourlyRateTarget> rates)
    {
        if (defaultCurrency is null)
            throw new DomainException("DefaultRateCurrency is required.");

        var exists = rates.Any(r => r.Currency.Value == defaultCurrency.Value);
        if (!exists)
            throw new DomainException($"DefaultRateCurrency '{defaultCurrency.Value}' must exist in HourlyRateTargets.");
    }

    private static void ValidateWeeklyCapacity(decimal? weeklyCapacityHours)
    {
        if (weeklyCapacityHours.HasValue && (weeklyCapacityHours.Value <= 0 || weeklyCapacityHours.Value > 168))
            throw new DomainException("WeeklyCapacityHours must be between 0 (exclusive) and 168 (inclusive).");
    }

    public static FreelancerProfile Rehydrate(
        FreelancerProfileId id,
        IEnumerable<SkillCapability> skills,
        IEnumerable<HourlyRateTarget> hourlyRateTargets,
        CurrencyCode defaultRateCurrency,
        decimal? weeklyCapacityHours = null)
    {
        var distinctSkills = ValidateSkills(skills);
        var distinctRates = ValidateRates(hourlyRateTargets);
        ValidateDefaultCurrency(defaultRateCurrency, distinctRates);

        return new FreelancerProfile(
            id,
            distinctSkills,
            distinctRates,
            defaultRateCurrency,
            weeklyCapacityHours);
    }
}

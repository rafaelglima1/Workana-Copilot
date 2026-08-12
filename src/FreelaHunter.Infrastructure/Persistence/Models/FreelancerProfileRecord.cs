namespace FreelaHunter.Infrastructure.Persistence.Models;

public sealed class FreelancerProfileRecord
{
    public Guid Id { get; set; }
    public string DefaultRateCurrency { get; set; } = string.Empty;
    public ICollection<FreelancerProfileSkillRecord> Skills { get; set; } = new List<FreelancerProfileSkillRecord>();
    public ICollection<FreelancerHourlyRateRecord> HourlyRates { get; set; } = new List<FreelancerHourlyRateRecord>();
}

public sealed class FreelancerProfileSkillRecord
{
    public Guid FreelancerProfileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public FreelancerProfileRecord FreelancerProfile { get; set; } = null!;
}

public sealed class FreelancerHourlyRateRecord
{
    public Guid FreelancerProfileId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public FreelancerProfileRecord FreelancerProfile { get; set; } = null!;
}

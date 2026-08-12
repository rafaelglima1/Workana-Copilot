using System.Security.Cryptography;
using System.Text;
using FreelaHunter.Domain.Entities;

namespace FreelaHunter.Application;

public static class FreelancerProfileFingerprintCalculator
{
    public static string Calculate(FreelancerProfile profile)
    {
        var sb = new StringBuilder();

        var sortedSkills = profile.Skills
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var skill in sortedSkills)
        {
            sb.Append(skill.Name);
            sb.Append('|');
            sb.Append(skill.Level);
            sb.Append('|');
        }

        var sortedRates = profile.HourlyRateTargets
            .OrderBy(r => r.Currency.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var rate in sortedRates)
        {
            sb.Append(rate.Currency.Value);
            sb.Append('|');
            sb.Append(rate.Amount);
            sb.Append('|');
        }

        sb.Append(profile.DefaultRateCurrency.Value);

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

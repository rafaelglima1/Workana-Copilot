using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class SaveFreelancerProfileHandler
{
    private readonly IFreelancerProfileRepository _repository;

    public SaveFreelancerProfileHandler(IFreelancerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<FreelancerProfileResponse> HandleAsync(
        SaveFreelancerProfileRequest request,
        CancellationToken cancellationToken)
    {
        var skills = request.Skills
            .Select(s => SkillCapability.Create(s.Name, MapSkillLevel(s.Level)))
            .ToList();

        var rates = request.HourlyRateTargets
            .Select(r => HourlyRateTarget.Create(r.Amount, CurrencyCode.From(r.Currency)))
            .ToList();

        var defaultCurrency = CurrencyCode.From(request.DefaultRateCurrency);

        var existing = await _repository.GetCurrentAsync(cancellationToken);

        if (existing is not null)
        {
            existing.Update(skills, rates, defaultCurrency);
            await _repository.SaveAsync(existing, cancellationToken);
            return MapToResponse(existing);
        }

        var profile = Domain.Entities.FreelancerProfile.Create(skills, rates, defaultCurrency);
        await _repository.SaveAsync(profile, cancellationToken);
        return MapToResponse(profile);
    }

    private static SkillLevel MapSkillLevel(ContractSkillLevel contract) => contract switch
    {
        ContractSkillLevel.None => SkillLevel.None,
        ContractSkillLevel.Basic => SkillLevel.Basic,
        ContractSkillLevel.Working => SkillLevel.Working,
        ContractSkillLevel.Strong => SkillLevel.Strong,
        ContractSkillLevel.Expert => SkillLevel.Expert,
        _ => SkillLevel.None
    };

    private static FreelancerProfileResponse MapToResponse(Domain.Entities.FreelancerProfile profile)
    {
        return new FreelancerProfileResponse
        {
            Id = profile.Id.Value,
            Skills = profile.Skills.Select(s => new ProfileSkillResponse
            {
                Name = s.Name,
                Level = s.Level.ToString()
            }).ToList(),
            HourlyRateTargets = profile.HourlyRateTargets.Select(r => new ProfileRateResponse
            {
                Amount = r.Amount,
                Currency = r.Currency.Value
            }).ToList(),
            DefaultRateCurrency = profile.DefaultRateCurrency.Value
        };
    }
}

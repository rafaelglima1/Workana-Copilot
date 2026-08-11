using FreelaHunter.Contracts.FreelancerProfile;

namespace FreelaHunter.Application;

public class GetFreelancerProfileHandler
{
    private readonly IFreelancerProfileRepository _repository;

    public GetFreelancerProfileHandler(IFreelancerProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<FreelancerProfileResponse?> HandleAsync(
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetCurrentAsync(cancellationToken);

        if (profile is null)
            return null;

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

using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;

namespace FreelaHunter.Application.Tests;

public class SaveFreelancerProfileTests
{
    private readonly FakeFreelancerProfileRepository _repository = new();
    private readonly SaveFreelancerProfileHandler _handler;

    public SaveFreelancerProfileTests()
    {
        _handler = new SaveFreelancerProfileHandler(_repository);
    }

    [Fact]
    public async Task CreatesProfile_WhenNoneExists()
    {
        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Single(result.Skills);
        Assert.Single(result.HourlyRateTargets);
        Assert.Equal("BRL", result.DefaultRateCurrency);
    }

    [Fact]
    public async Task UpdatesProfile_WhenExists()
    {
        await _handler.HandleAsync(CreateValidRequest(), CancellationToken.None);

        var updateRequest = new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "Flutter", Level = ContractSkillLevel.Strong }],
            HourlyRateTargets = [new ProfileRateInput { Amount = 50, Currency = "USD" }],
            DefaultRateCurrency = "USD"
        };

        var result = await _handler.HandleAsync(updateRequest, CancellationToken.None);

        Assert.Single(result.Skills);
        Assert.Equal("Flutter", result.Skills[0].Name);
    }

    [Fact]
    public async Task PreservesId_OnUpdate()
    {
        var first = await _handler.HandleAsync(CreateValidRequest(), CancellationToken.None);

        var updateRequest = new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert }],
            HourlyRateTargets = [new ProfileRateInput { Amount = 200, Currency = "BRL" }],
            DefaultRateCurrency = "BRL"
        };

        var second = await _handler.HandleAsync(updateRequest, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task MapsSkillLevel_Correctly()
    {
        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal("Expert", result.Skills[0].Level);
    }

    [Fact]
    public async Task MapsHourlyRate_Correctly()
    {
        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(100, result.HourlyRateTargets[0].Amount);
        Assert.Equal("BRL", result.HourlyRateTargets[0].Currency);
    }

    [Fact]
    public async Task MapsDefaultCurrency_Correctly()
    {
        var request = CreateValidRequest();

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal("BRL", result.DefaultRateCurrency);
    }

    [Fact]
    public async Task RepositorySaveCalled()
    {
        await _handler.HandleAsync(CreateValidRequest(), CancellationToken.None);

        Assert.NotNull(_repository.SavedProfile);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task DomainNormalizations_Reflected()
    {
        var request = new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "  c#  ", Level = ContractSkillLevel.Expert }],
            HourlyRateTargets = [new ProfileRateInput { Amount = 100, Currency = "  brl  " }],
            DefaultRateCurrency = "  BRL  "
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal("c#", result.Skills[0].Name);
        Assert.Equal("BRL", result.HourlyRateTargets[0].Currency);
        Assert.Equal("BRL", result.DefaultRateCurrency);
    }

    private static SaveFreelancerProfileRequest CreateValidRequest()
    {
        return new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert }],
            HourlyRateTargets = [new ProfileRateInput { Amount = 100, Currency = "BRL" }],
            DefaultRateCurrency = "BRL"
        };
    }
}

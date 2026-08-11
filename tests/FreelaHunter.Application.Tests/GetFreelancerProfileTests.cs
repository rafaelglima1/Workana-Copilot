namespace FreelaHunter.Application.Tests;

public class GetFreelancerProfileTests
{
    private readonly FakeFreelancerProfileRepository _repository = new();
    private readonly GetFreelancerProfileHandler _handler;

    public GetFreelancerProfileTests()
    {
        _handler = new GetFreelancerProfileHandler(_repository);
    }

    [Fact]
    public async Task ExistingProfile_ReturnsProfile()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _repository.SaveAsync(profile, CancellationToken.None);

        var result = await _handler.HandleAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(profile.Id.Value, result!.Id);
        Assert.Equal(3, result.Skills.Count);
        Assert.Equal(2, result.HourlyRateTargets.Count);
        Assert.Equal("BRL", result.DefaultRateCurrency);
    }

    [Fact]
    public async Task NonExistingProfile_ReturnsNull()
    {
        var result = await _handler.HandleAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Response_IsComplete()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _repository.SaveAsync(profile, CancellationToken.None);

        var result = await _handler.HandleAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.Id);
        Assert.False(string.IsNullOrEmpty(result.DefaultRateCurrency));
        Assert.All(result.Skills, s => Assert.False(string.IsNullOrEmpty(s.Name)));
        Assert.All(result.Skills, s => Assert.False(string.IsNullOrEmpty(s.Level)));
        Assert.All(result.HourlyRateTargets, r => Assert.True(r.Amount > 0));
        Assert.All(result.HourlyRateTargets, r => Assert.False(string.IsNullOrEmpty(r.Currency)));
    }
}

using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Application;
using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FreelaHunter.Api.Tests;

public class FreelancerProfileApiTests : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var factory = new WebApplicationFactory<Program>();
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IFreelancerProfileRepository));
                if (descriptor is not null)
                    services.Remove(descriptor);
                services.AddSingleton<IFreelancerProfileRepository, InMemoryFreelancerProfileRepository>();
            });
        }).CreateClient();
        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetProfile_BeforeConfigure_Returns404()
    {
        var response = await _client.GetAsync("/api/freelancer-profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_Valid_Returns200()
    {
        var request = CreateValidRequest();

        var response = await _client.PutAsJsonAsync("/api/freelancer-profile", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_AfterPut_Returns200()
    {
        var request = CreateValidRequest();
        await _client.PutAsJsonAsync("/api/freelancer-profile", request);

        var response = await _client.GetAsync("/api/freelancer-profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_ReturnsValidProfile()
    {
        var request = CreateValidRequest();

        var putResponse = await _client.PutAsJsonAsync("/api/freelancer-profile", request);
        var result = await putResponse.Content.ReadFromJsonAsync<FreelancerProfileResponse>();

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.Id);
        Assert.Single(result.Skills);
        Assert.Single(result.HourlyRateTargets);
        Assert.Equal("BRL", result.DefaultRateCurrency);
    }

    [Fact]
    public async Task PutProfile_PreservesId()
    {
        var request = CreateValidRequest();
        var first = await _client.PutAsJsonAsync("/api/freelancer-profile", request);
        var firstResult = await first.Content.ReadFromJsonAsync<FreelancerProfileResponse>();

        var second = await _client.PutAsJsonAsync("/api/freelancer-profile", request);
        var secondResult = await second.Content.ReadFromJsonAsync<FreelancerProfileResponse>();

        Assert.Equal(firstResult!.Id, secondResult!.Id);
    }

    [Fact]
    public async Task PutProfile_InvalidSkill_Returns400()
    {
        var request = new SaveFreelancerProfileRequest
        {
            Skills = [],
            HourlyRateTargets = [new ProfileRateInput { Amount = 100, Currency = "BRL" }],
            DefaultRateCurrency = "BRL"
        };

        var response = await _client.PutAsJsonAsync("/api/freelancer-profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_InvalidRate_Returns400()
    {
        var request = new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert }],
            HourlyRateTargets = [],
            DefaultRateCurrency = "BRL"
        };

        var response = await _client.PutAsJsonAsync("/api/freelancer-profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_DefaultCurrencyNotInRates_Returns400()
    {
        var request = new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert }],
            HourlyRateTargets = [new ProfileRateInput { Amount = 100, Currency = "BRL" }],
            DefaultRateCurrency = "USD"
        };

        var response = await _client.PutAsJsonAsync("/api/freelancer-profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

public class ScoreWithProfileApiTests : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var factory = new WebApplicationFactory<Program>();
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IFreelancerProfileRepository));
                if (descriptor is not null)
                    services.Remove(descriptor);
                services.AddSingleton<IFreelancerProfileRepository, InMemoryFreelancerProfileRepository>();
            });
        }).CreateClient();
        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ScoreWithProfile_NoProfile_Returns409()
    {
        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_NonExistingOpportunity_Returns404()
    {
        await SetupProfile();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{Guid.NewGuid()}/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_InvalidId_Returns400()
    {
        await SetupProfile();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await _client.PostAsJsonAsync(
            "/api/opportunities/00000000-0000-0000-0000-000000000000/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_FullFlow_Returns200()
    {
        await SetupProfile();

        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_ReturnsValidResponse()
    {
        await SetupProfile();

        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/score-with-profile", scoreRequest);
        var result = await response.Content.ReadFromJsonAsync<OpportunityScoreResponse>();

        Assert.NotNull(result);
        Assert.InRange(result!.Overall, 0, 100);
        Assert.False(string.IsNullOrEmpty(result.Recommendation));
        Assert.NotNull(result.TechnicalFit);
        Assert.NotNull(result.EconomicPotential);
        Assert.NotNull(result.ClientQuality);
        Assert.NotNull(result.Competition);
        Assert.NotNull(result.DeliveryConfidence);
        Assert.NotNull(result.AiLeverage);
    }

    [Fact]
    public async Task ExistingEndpoints_StillWork()
    {
        var healthResponse = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var createRequest = CreateManualRequest();
        var postResponse = await _client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();
        var getResponse = await _client.GetAsync($"/api/opportunities/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    private async Task SetupProfile()
    {
        var profileRequest = new SaveFreelancerProfileRequest
        {
            Skills = [
                new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert },
                new ProfileSkillInput { Name = "API", Level = ContractSkillLevel.Expert },
                new ProfileSkillInput { Name = "Flutter", Level = ContractSkillLevel.None }
            ],
            HourlyRateTargets = [
                new ProfileRateInput { Amount = 100, Currency = "BRL" },
                new ProfileRateInput { Amount = 65, Currency = "USD" }
            ],
            DefaultRateCurrency = "BRL"
        };

        await _client.PutAsJsonAsync("/api/freelancer-profile", profileRequest);
    }

    private static CreateManualOpportunityRequest CreateManualRequest()
    {
        return new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test CRUD API",
            Description = "Build a REST API with ASP.NET Core",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Range,
                Basis = ContractBudgetBasis.FixedProject,
                Minimum = 5000,
                Maximum = 15000,
                Currency = "BRL"
            },
            RequiredSkills = ["C#", "API", "Flutter"]
        };
    }
}

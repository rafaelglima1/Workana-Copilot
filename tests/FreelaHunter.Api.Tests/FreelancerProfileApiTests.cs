using System.Net;
using System.Net.Http.Json;
using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FreelaHunter.Api.Tests;

[Collection("Integration")]
public class FreelancerProfileApiTests
{
    [Fact]
    public async Task GetProfile_BeforeConfigure_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/freelancer-profile");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_Valid_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = CreateValidRequest();

        var response = await client.PutAsJsonAsync("/api/freelancer-profile", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_AfterPut_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = CreateValidRequest();
        await client.PutAsJsonAsync("/api/freelancer-profile", request);

        var response = await client.GetAsync("/api/freelancer-profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_ReturnsValidProfile()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = CreateValidRequest();

        var putResponse = await client.PutAsJsonAsync("/api/freelancer-profile", request);
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
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = CreateValidRequest();

        var first = await client.PutAsJsonAsync("/api/freelancer-profile", request);
        var firstResult = await first.Content.ReadFromJsonAsync<FreelancerProfileResponse>();

        var second = await client.PutAsJsonAsync("/api/freelancer-profile", request);
        var secondResult = await second.Content.ReadFromJsonAsync<FreelancerProfileResponse>();

        Assert.Equal(firstResult!.Id, secondResult!.Id);
    }

    [Fact]
    public async Task PutProfile_InvalidSkill_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new SaveFreelancerProfileRequest
        {
            Skills = [],
            HourlyRateTargets = [new ProfileRateInput { Amount = 100, Currency = "BRL" }],
            DefaultRateCurrency = "BRL"
        };

        var response = await client.PutAsJsonAsync("/api/freelancer-profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_InvalidRate_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert }],
            HourlyRateTargets = [],
            DefaultRateCurrency = "BRL"
        };

        var response = await client.PutAsJsonAsync("/api/freelancer-profile", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutProfile_DefaultCurrencyNotInRates_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var request = new SaveFreelancerProfileRequest
        {
            Skills = [new ProfileSkillInput { Name = "C#", Level = ContractSkillLevel.Expert }],
            HourlyRateTargets = [new ProfileRateInput { Amount = 100, Currency = "BRL" }],
            DefaultRateCurrency = "USD"
        };

        var response = await client.PutAsJsonAsync("/api/freelancer-profile", request);

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

[Collection("Integration")]
public class ScoreWithProfileApiTests
{
    [Fact]
    public async Task ScoreWithProfile_NoProfile_Returns409()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_NonExistingOpportunity_Returns404()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{Guid.NewGuid()}/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_InvalidId_Returns400()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await client.PostAsJsonAsync(
            "/api/opportunities/00000000-0000-0000-0000-000000000000/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_FullFlow_Returns200()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);

        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await client.PostAsJsonAsync(
            $"/api/opportunities/{created!.Id}/score-with-profile", scoreRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ScoreWithProfile_ReturnsValidResponse()
    {
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        await SetupProfile(client);

        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();

        var scoreRequest = new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };

        var response = await client.PostAsJsonAsync(
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
        await using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);

        var createRequest = CreateManualRequest();
        var postResponse = await client.PostAsJsonAsync("/api/opportunities/manual", createRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var created = await postResponse.Content.ReadFromJsonAsync<OpportunityResponse>();
        var getResponse = await client.GetAsync($"/api/opportunities/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    private static async Task SetupProfile(HttpClient client)
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

        await client.PutAsJsonAsync("/api/freelancer-profile", profileRequest);
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

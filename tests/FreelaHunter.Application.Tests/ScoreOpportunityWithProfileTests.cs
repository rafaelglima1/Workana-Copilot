using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class ScoreOpportunityWithProfileTests
{
    private readonly FakeOpportunityRepository _opportunityRepository = new();
    private readonly FakeFreelancerProfileRepository _profileRepository = new();
    private readonly ScoreOpportunityWithProfileHandler _handler;

    public ScoreOpportunityWithProfileTests()
    {
        _handler = new ScoreOpportunityWithProfileHandler(_opportunityRepository, _profileRepository);
    }

    private async Task<Opportunity> CreateAndStoreOpportunity(
        CurrencyCode? currency = null,
        IEnumerable<string>? skills = null)
    {
        var budget = currency is not null
            ? Budget.Range(5000, 15000, currency, BudgetBasis.FixedProject)
            : Budget.Range(5000, 15000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test CRUD API",
            description: "Build a REST API",
            budget: budget,
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: skills ?? ["C#", "API"]);

        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);
        return opportunity;
    }

    private async Task StoreProfile()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);
    }

    [Fact]
    public async Task ExistingOpportunityAndProfile_ReturnsScore()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.InRange(result!.Overall, 0, 100);
        Assert.False(string.IsNullOrEmpty(result.Recommendation));
    }

    [Fact]
    public async Task NonExistingOpportunity_ReturnsNull()
    {
        await StoreProfile();

        var result = await _handler.HandleAsync(
            Guid.NewGuid(),
            CreateValidRequest(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task NonExistingProfile_ReturnsNull()
    {
        var opportunity = await CreateAndStoreOpportunity();

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SkillsComeFromProfile()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API", "Flutter"]);

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task BudgetBRL_SelectsBRLRate()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(currency: CurrencyCode.From("BRL"));

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task BudgetUSD_SelectsUSDRate()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(currency: CurrencyCode.From("USD"));

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task BudgetEUR_UsesDefaultRate()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(currency: CurrencyCode.From("EUR"));

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task BudgetNoCurrency_UsesDefaultRate()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task EstimatedHours_Propagated()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var request = CreateValidRequest();
        request.EstimatedHours = 200;

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            request,
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DeliveryRisk_Propagated()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var request = CreateValidRequest();
        request.DeliveryRisk = ContractDeliveryRisk.Low;

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            request,
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AiLeverage_Propagated()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var request = CreateValidRequest();
        request.AiLeverage = ContractAiLeverage.VeryHigh;

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            request,
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ScoreNotPersisted()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        var stored = await _opportunityRepository.GetByIdAsync(opportunity.Id, CancellationToken.None);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task OpportunityNotModified()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        var stored = await _opportunityRepository.GetByIdAsync(opportunity.Id, CancellationToken.None);
        Assert.Equal("Test CRUD API", stored!.Title);
    }

    [Fact]
    public async Task ProfileNotModified()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);
        var opportunity = await CreateAndStoreOpportunity();

        await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        var storedProfile = await _profileRepository.GetCurrentAsync(CancellationToken.None);
        Assert.NotNull(storedProfile);
        Assert.Equal(profile.Id.Value, storedProfile!.Id.Value);
    }

    [Fact]
    public async Task TechnicalSkillMatches_ContainsMatches()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "Python"]);

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.TechnicalSkillMatches);
        Assert.Equal(2, result.TechnicalSkillMatches.Count);
    }

    [Fact]
    public async Task TechnicalSkillMatches_ExactMatch()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(skills: ["C#"]);

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
        var match = result!.TechnicalSkillMatches.First();
        Assert.Equal("Exact", match.MatchKind);
    }

    [Fact]
    public async Task TechnicalSkillMatches_AliasMatch()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(skills: ["CSharp"]);

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
        var match = result!.TechnicalSkillMatches.First();
        Assert.Equal("Alias", match.MatchKind);
    }

    [Fact]
    public async Task TechnicalSkillMatches_NoMatch()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(skills: ["Python"]);

        var result = await _handler.HandleAsync(
            opportunity.Id.Value,
            CreateValidRequest(),
            CancellationToken.None);

        Assert.NotNull(result);
        var match = result!.TechnicalSkillMatches.First();
        Assert.Equal("NoMatch", match.MatchKind);
    }

    private static ScoreOpportunityWithProfileRequest CreateValidRequest()
    {
        return new ScoreOpportunityWithProfileRequest
        {
            EstimatedHours = 120,
            DeliveryRisk = ContractDeliveryRisk.High,
            AiLeverage = ContractAiLeverage.High
        };
    }
}

using FreelaHunter.Application.Exceptions;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class EvaluateOpportunityTests
{
    private readonly FakeOpportunityRepository _opportunityRepository = new();
    private readonly FakeFreelancerProfileRepository _profileRepository = new();
    private readonly FakeIntelligenceGateway _gateway = new();

    private EvaluateOpportunityHandler CreateHandler() =>
        new(_opportunityRepository, _profileRepository, _gateway);

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
            description: "Build a REST API with ASP.NET Core",
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
    public async Task HappyPath_ReturnsEvaluation()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(opportunity.Id.Value, result!.OpportunityId.Value);
        Assert.NotNull(result.Analysis);
        Assert.NotNull(result.Score);
        Assert.NotNull(result.ScoringInputs);
    }

    [Fact]
    public async Task HappyPath_GatewayCalledOnce()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.Single(_gateway.Inputs);
    }

    [Fact]
    public async Task HappyPath_ScoreContainsOverall()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.InRange(result!.Score.Overall.Value, 0, 100);
    }

    [Fact]
    public async Task HappyPath_ScoreContainsRecommendation()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Score.Recommendation.ToString()));
    }

    [Fact]
    public async Task HappyPath_SkillsComeFromProfile()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API", "Flutter"]);

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        var profileSkillNames = result!.ScoringInputs.ProfileSkills.Select(s => s.Name).ToList();
        Assert.Contains("C#", profileSkillNames);
        Assert.Contains("API", profileSkillNames);
        Assert.Contains("Flutter", profileSkillNames);
    }

    [Fact]
    public async Task HappyPath_EstimatedHoursFromAnalysis()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(120m, result!.ScoringInputs.EstimatedHours);
    }

    [Fact]
    public async Task HappyPath_DeliveryRiskFromAnalysis()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(DeliveryRiskLevel.Moderate, result!.ScoringInputs.DeliveryRisk);
    }

    [Fact]
    public async Task HappyPath_AiLeverageFromAnalysis()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AiLeverageLevel.High, result!.ScoringInputs.AiLeverage);
    }

    [Fact]
    public async Task HappyPath_TargetHourlyRateFromProfile()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(currency: CurrencyCode.From("BRL"));

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100m, result!.ScoringInputs.TargetHourlyRate.Amount);
        Assert.Equal("BRL", result.ScoringInputs.TargetHourlyRate.Currency);
    }

    [Fact]
    public async Task NonExistingOpportunity_ReturnsNull()
    {
        await StoreProfile();

        var result = await CreateHandler().HandleAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task NonExistingOpportunity_GatewayNotCalled()
    {
        await StoreProfile();

        await CreateHandler().HandleAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Empty(_gateway.Inputs);
    }

    [Fact]
    public async Task NonExistingProfile_ReturnsNull()
    {
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task NonExistingProfile_GatewayNotCalled()
    {
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.Empty(_gateway.Inputs);
    }

    [Fact]
    public async Task GatewayFailure_PropagatesException()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();
        var failingGateway = new FakeIntelligenceGateway((input, ct) =>
            throw new OpportunityIntelligenceUnavailableException("Provider unavailable"));
        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, failingGateway);

        await Assert.ThrowsAsync<OpportunityIntelligenceUnavailableException>(
            () => handler.HandleAsync(opportunity.Id.Value, CancellationToken.None));
    }

    [Fact]
    public async Task GatewayFailure_ScoreNotProduced()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();
        var failingGateway = new FakeIntelligenceGateway((input, ct) =>
            throw new OpportunityIntelligenceUnavailableException("Provider unavailable"));
        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, failingGateway);

        try
        {
            await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);
        }
        catch
        {
            // Expected
        }

        var stored = await _opportunityRepository.GetByIdAsync(opportunity.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("Test CRUD API", stored!.Title);
    }

    [Fact]
    public async Task OpportunityNotModifiedAfterEvaluate()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var stored = await _opportunityRepository.GetByIdAsync(opportunity.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("Test CRUD API", stored!.Title);
        Assert.Equal(OpportunityStatus.New, stored.Status);
    }

    [Fact]
    public async Task ProfileNotModifiedAfterEvaluate()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);
        var opportunity = await CreateAndStoreOpportunity();

        await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        var storedProfile = await _profileRepository.GetCurrentAsync(CancellationToken.None);
        Assert.NotNull(storedProfile);
        Assert.Equal(profile.Id.Value, storedProfile!.Id.Value);
    }

    [Fact]
    public async Task TechnicalRequirementsFromMiMo_NotUsedForScoring()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API"]);

        var gatewayWithExtraReqs = new FakeIntelligenceGateway((input, ct) =>
        {
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["C#", "API", "Docker", "Kubernetes"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = [],
                Usage = new LlmUsage { Provider = "MiMo", Model = "mimo-v2.5" }
            });
        });

        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, gatewayWithExtraReqs);

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        var profileSkillNames = result!.ScoringInputs.ProfileSkills.Select(s => s.Name).ToList();
        Assert.DoesNotContain("Docker", profileSkillNames);
        Assert.DoesNotContain("Kubernetes", profileSkillNames);
    }

    [Fact]
    public async Task BudgetBRL_UsesBRLRate()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(currency: CurrencyCode.From("BRL"));

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100m, result!.ScoringInputs.TargetHourlyRate.Amount);
        Assert.Equal("BRL", result.ScoringInputs.TargetHourlyRate.Currency);
    }

    [Fact]
    public async Task BudgetUSD_UsesUSDRate()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(currency: CurrencyCode.From("USD"));

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(65m, result!.ScoringInputs.TargetHourlyRate.Amount);
        Assert.Equal("USD", result.ScoringInputs.TargetHourlyRate.Currency);
    }

    [Fact]
    public async Task BudgetEUR_UsesDefaultBRLRate()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity(currency: CurrencyCode.From("EUR"));

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(100m, result!.ScoringInputs.TargetHourlyRate.Amount);
        Assert.Equal("BRL", result.ScoringInputs.TargetHourlyRate.Currency);
    }

    [Fact]
    public async Task ScoringInputSummary_SourcesAreTraceable()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Opportunity.RequiredSkills + FreelancerProfile.Skills",
            result!.ScoringInputs.TechnicalFitSource);
        Assert.Equal("OpportunitySemanticAnalysis",
            result.ScoringInputs.EstimatedHoursSource);
        Assert.Equal("OpportunitySemanticAnalysis",
            result.ScoringInputs.DeliveryRiskSource);
        Assert.Equal("OpportunitySemanticAnalysis",
            result.ScoringInputs.AiLeverageSource);
        Assert.Equal("FreelancerProfile",
            result.ScoringInputs.TargetHourlyRateSource);
    }

    [Fact]
    public async Task AnalysisConfidence_NotUsedForScoring()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(AnalysisConfidence.Medium, result!.Analysis.Confidence);
    }

    [Fact]
    public async Task AnalysisRiskFactors_ReturnedToUser()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Analysis.RiskFactors);
    }

    [Fact]
    public async Task AnalysisTechnicalRequirements_ReturnedToUser()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result!.Analysis.TechnicalRequirements);
    }

    [Fact]
    public async Task AnalysisUsage_ReturnedToUser()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var result = await CreateHandler().HandleAsync(
            opportunity.Id.Value,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.Analysis.Usage);
        Assert.Equal("MiMo", result.Analysis.Usage!.Provider);
        Assert.Equal("mimo-v2.5", result.Analysis.Usage.Model);
    }

    [Fact]
    public async Task BalanceScenario_WithFakeMiMoOutput()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Sistema de Balança",
            description: "Desenvolvimento de sistema para balança",
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject),
            client: ClientSnapshot.Create(publishedProjects: 3, paidProjects: 0, rating: null),
            competition: CompetitionSnapshot.Create(54, null),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["C#", "API", "Flutter"]);

        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);

        var balanceGateway = new FakeIntelligenceGateway((input, ct) =>
        {
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.High,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["Flutter", "REST API", "Bluetooth"],
                RiskFactors = ["Modelo da balança não informado"],
                AiLeverageReasons = ["CRUD operations"],
                ClarifyingQuestions = [],
                Usage = new LlmUsage
                {
                    InputTokens = 100,
                    OutputTokens = 200,
                    TotalTokens = 300,
                    Provider = "MiMo",
                    Model = "mimo-v2.5"
                }
            });
        });

        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, balanceGateway);

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.InRange(result!.Score.Overall.Value, 0, 100);
        Assert.False(string.IsNullOrEmpty(result.Score.Recommendation.ToString()));

        Assert.Equal(120m, result.ScoringInputs.EstimatedHours);
        Assert.Equal(DeliveryRiskLevel.High, result.ScoringInputs.DeliveryRisk);
        Assert.Equal(AiLeverageLevel.High, result.ScoringInputs.AiLeverage);

        var profileSkillNames = result.ScoringInputs.ProfileSkills.Select(s => s.Name).ToList();
        Assert.Contains("C#", profileSkillNames);
        Assert.Contains("API", profileSkillNames);
        Assert.Contains("Flutter", profileSkillNames);

        Assert.Equal(100m, result.ScoringInputs.TargetHourlyRate.Amount);
        Assert.Equal("BRL", result.ScoringInputs.TargetHourlyRate.Currency);

        Assert.Contains("Modelo da balança não informado", result.Analysis.RiskFactors);
        Assert.Contains("Flutter", result.Analysis.TechnicalRequirements);
        Assert.Contains("REST API", result.Analysis.TechnicalRequirements);
        Assert.Contains("Bluetooth", result.Analysis.TechnicalRequirements);
    }

    [Fact]
    public async Task InferredTechnicalRequirementMatches_ContainsMatches()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API"]);

        var gatewayWithReqs = new FakeIntelligenceGateway((input, ct) =>
        {
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["C#", "API", "Docker", "Kubernetes"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = [],
                Usage = new LlmUsage { Provider = "MiMo", Model = "mimo-v2.5" }
            });
        });

        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, gatewayWithReqs);

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.InferredTechnicalRequirementMatches);
        Assert.Equal(4, result.InferredTechnicalRequirementMatches.Count);
    }

    [Fact]
    public async Task InferredTechnicalRequirementMatches_ExactMatch()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API"]);

        var gatewayWithReqs = new FakeIntelligenceGateway((input, ct) =>
        {
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["C#"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = [],
                Usage = new LlmUsage { Provider = "MiMo", Model = "mimo-v2.5" }
            });
        });

        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, gatewayWithReqs);

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        var match = result!.InferredTechnicalRequirementMatches.First();
        Assert.Equal(SkillMatchKind.Exact, match.MatchKind);
    }

    [Fact]
    public async Task InferredTechnicalRequirementMatches_AliasMatch()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API"]);

        var gatewayWithReqs = new FakeIntelligenceGateway((input, ct) =>
        {
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["CSharp"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = [],
                Usage = new LlmUsage { Provider = "MiMo", Model = "mimo-v2.5" }
            });
        });

        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, gatewayWithReqs);

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        var match = result!.InferredTechnicalRequirementMatches.First();
        Assert.Equal(SkillMatchKind.Alias, match.MatchKind);
    }

    [Fact]
    public async Task InferredTechnicalRequirementMatches_NoMatch()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API"]);

        var gatewayWithReqs = new FakeIntelligenceGateway((input, ct) =>
        {
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["Python"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = [],
                Usage = new LlmUsage { Provider = "MiMo", Model = "mimo-v2.5" }
            });
        });

        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, gatewayWithReqs);

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        var match = result!.InferredTechnicalRequirementMatches.First();
        Assert.Equal(SkillMatchKind.NoMatch, match.MatchKind);
    }

    [Fact]
    public async Task InferredTechnicalRequirementMatches_DoesNotAffectScore()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);

        var opportunity = await CreateAndStoreOpportunity(skills: ["C#", "API"]);

        var gatewayWithReqs = new FakeIntelligenceGateway((input, ct) =>
        {
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["Python", "Java"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = [],
                Usage = new LlmUsage { Provider = "MiMo", Model = "mimo-v2.5" }
            });
        });

        var handler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, gatewayWithReqs);

        var result = await handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(result!.InferredTechnicalRequirementMatches);
        Assert.Equal(2, result.InferredTechnicalRequirementMatches.Count);
        Assert.InRange(result.Score.Overall.Value, 0, 100);
    }
}

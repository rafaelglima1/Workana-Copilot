using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class OpportunityBackgroundProcessorTests
{
    private readonly FakeOpportunityRepository _opportunityRepository = new();
    private readonly FakeFreelancerProfileRepository _profileRepository = new();
    private readonly FakeIntelligenceGateway _gateway = new();
    private readonly FakeOpportunityEvaluationSnapshotRepository _snapshotRepository = new();
    private readonly FakeOpportunityQueryRepository _queryRepository = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private async Task StoreProfile()
    {
        var profile = FakeFreelancerProfileRepository.CreateDefaultProfile();
        await _profileRepository.SaveAsync(profile, CancellationToken.None);
    }

    private async Task<Opportunity> CreateAndStoreOpportunity(
        string title = "Test Opportunity",
        IEnumerable<string>? skills = null)
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: title,
            description: "Build a REST API with ASP.NET Core",
            budget: Budget.Range(5000, 15000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject),
            client: ClientSnapshot.Create(publishedProjects: 5, paidProjects: 5, rating: 4.5m),
            competition: CompetitionSnapshot.Create(10, 20),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: skills ?? ["C#", "API"]);

        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);
        return opportunity;
    }

    private async Task<Opportunity> CreateAndStoreLowScoreOpportunity()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Niche Language Project",
            description: "Build a legacy COBOL system",
            budget: Budget.Range(500, 1000, CurrencyCode.From("BRL"), BudgetBasis.FixedProject),
            client: ClientSnapshot.Create(publishedProjects: 0, paidProjects: 0, rating: null),
            competition: CompetitionSnapshot.Create(100, null),
            capturedAt: DateTimeOffset.UtcNow,
            requiredSkills: ["COBOL", "Legacy"]);

        await _opportunityRepository.AddAsync(opportunity, CancellationToken.None);
        return opportunity;
    }

    private OpportunityBackgroundProcessor CreateProcessor()
    {
        var listHandler = new ListOpportunitiesHandler(_queryRepository);
        var evaluateHandler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, _gateway, _snapshotRepository, _timeProvider);
        var proposalHandler = new GenerateProposalStrategyHandler(
            _opportunityRepository, _profileRepository, _snapshotRepository);

        return new OpportunityBackgroundProcessor(listHandler, evaluateHandler, proposalHandler);
    }

    [Fact]
    public async Task EmptyInbox_ReturnsZero()
    {
        await StoreProfile();

        var processor = CreateProcessor();
        var result = await processor.ProcessPendingAsync(10, CancellationToken.None);

        Assert.Equal(0, result.Evaluated);
        Assert.Equal(0, result.ProposalsGenerated);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task SingleUnevaluatedOpportunity_EvaluatesAndGeneratesProposal()
    {
        await StoreProfile();
        var opportunity = await CreateAndStoreOpportunity();

        var items = new List<OpportunityInboxItem>
        {
            CreateInboxItem(opportunity.Id.Value)
        };
        _queryRepository.SetResult(items);

        var processor = CreateProcessor();
        var result = await processor.ProcessPendingAsync(10, CancellationToken.None);

        Assert.Equal(1, result.Evaluated);
        Assert.Equal(0, result.Failed);
        Assert.Single(_snapshotRepository.SavedSnapshots);
        Assert.Equal(opportunity.Id.Value, _snapshotRepository.SavedSnapshots[0].OpportunityId);
    }

    [Fact]
    public async Task TwoUnevaluatedOpportunities_EvaluatesBoth()
    {
        await StoreProfile();
        var opp1 = await CreateAndStoreOpportunity("First Opportunity");
        var opp2 = await CreateAndStoreOpportunity("Second Opportunity");

        var items = new List<OpportunityInboxItem>
        {
            CreateInboxItem(opp1.Id.Value),
            CreateInboxItem(opp2.Id.Value)
        };
        _queryRepository.SetResult(items);

        var processor = CreateProcessor();
        var result = await processor.ProcessPendingAsync(10, CancellationToken.None);

        Assert.Equal(2, result.Evaluated);
        Assert.Equal(0, result.Failed);
        Assert.Equal(2, _snapshotRepository.SavedSnapshots.Count);
    }

    [Fact]
    public async Task TwoUnevaluatedOpportunities_ProposalsMatchRecommendation()
    {
        await StoreProfile();
        var opp1 = await CreateAndStoreOpportunity("First Opportunity");
        var opp2 = await CreateAndStoreLowScoreOpportunity();

        var items = new List<OpportunityInboxItem>
        {
            CreateInboxItem(opp1.Id.Value),
            CreateInboxItem(opp2.Id.Value)
        };
        _queryRepository.SetResult(items);

        var processor = CreateProcessor();
        var result = await processor.ProcessPendingAsync(10, CancellationToken.None);

        Assert.Equal(2, result.Evaluated);
        Assert.Equal(0, result.Failed);

        var goCount = _snapshotRepository.SavedSnapshots
            .Count(s => s.Recommendation is OpportunityRecommendation.Prioritize
                or OpportunityRecommendation.Apply);
        Assert.Equal(goCount, result.ProposalsGenerated);
    }

    [Fact]
    public async Task GatewayFailure_IncrementsFailedButContinues()
    {
        await StoreProfile();
        var opp1 = await CreateAndStoreOpportunity("First Opportunity");
        var opp2 = await CreateAndStoreOpportunity("Second Opportunity");

        var items = new List<OpportunityInboxItem>
        {
            CreateInboxItem(opp1.Id.Value),
            CreateInboxItem(opp2.Id.Value)
        };
        _queryRepository.SetResult(items);

        var failingGateway = new FakeIntelligenceGateway((input, ct) =>
            throw new InvalidOperationException("Gateway unavailable"));

        var listHandler = new ListOpportunitiesHandler(_queryRepository);
        var evaluateHandler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, failingGateway, _snapshotRepository, _timeProvider);
        var proposalHandler = new GenerateProposalStrategyHandler(
            _opportunityRepository, _profileRepository, _snapshotRepository);
        var processor = new OpportunityBackgroundProcessor(listHandler, evaluateHandler, proposalHandler);

        var result = await processor.ProcessPendingAsync(10, CancellationToken.None);

        Assert.Equal(0, result.Evaluated);
        Assert.Equal(2, result.Failed);
        Assert.Empty(_snapshotRepository.SavedSnapshots);
    }

    [Fact]
    public async Task PartialFailure_StillProcessesRemaining()
    {
        await StoreProfile();
        var opp1 = await CreateAndStoreOpportunity("First Opportunity");
        var opp2 = await CreateAndStoreOpportunity("Second Opportunity");

        var items = new List<OpportunityInboxItem>
        {
            CreateInboxItem(opp1.Id.Value),
            CreateInboxItem(opp2.Id.Value)
        };
        _queryRepository.SetResult(items);

        var gatewayThrowsOnSecond = new FakeIntelligenceGateway((input, ct) =>
        {
            if (input.Title == "Second Opportunity")
                throw new InvalidOperationException("Gateway unavailable");
            return Task.FromResult(new OpportunitySemanticAnalysis
            {
                EstimatedHours = 120,
                DeliveryRisk = DeliveryRiskLevel.Moderate,
                AiLeverage = AiLeverageLevel.High,
                Confidence = AnalysisConfidence.Medium,
                TechnicalRequirements = ["C#", "API"],
                RiskFactors = [],
                AiLeverageReasons = [],
                ClarifyingQuestions = [],
                Usage = new LlmUsage { Provider = "Test", Model = "test" }
            });
        });

        var listHandler = new ListOpportunitiesHandler(_queryRepository);
        var evaluateHandler = new EvaluateOpportunityHandler(
            _opportunityRepository, _profileRepository, gatewayThrowsOnSecond, _snapshotRepository, _timeProvider);
        var proposalHandler = new GenerateProposalStrategyHandler(
            _opportunityRepository, _profileRepository, _snapshotRepository);
        var processor = new OpportunityBackgroundProcessor(listHandler, evaluateHandler, proposalHandler);

        var result = await processor.ProcessPendingAsync(10, CancellationToken.None);

        Assert.Equal(1, result.Evaluated);
        Assert.Equal(1, result.Failed);
        Assert.Single(_snapshotRepository.SavedSnapshots);
    }

    [Fact]
    public async Task ListHandlerCalledWithEvaluatedFalse()
    {
        await StoreProfile();

        var processor = CreateProcessor();
        await processor.ProcessPendingAsync(10, CancellationToken.None);

        var criteria = _queryRepository.ReceivedCriteria.Single();
        Assert.False(criteria.Evaluated);
        Assert.Equal(OpportunitySort.CapturedAtDesc, criteria.Sort);
        Assert.Equal(10, criteria.PageSize);
    }

    [Fact]
    public async Task BatchSizeIsClamped()
    {
        await StoreProfile();

        var processor = CreateProcessor();
        await processor.ProcessPendingAsync(200, CancellationToken.None);

        var criteria = _queryRepository.ReceivedCriteria.Single();
        Assert.Equal(100, criteria.PageSize);
    }

    private static OpportunityInboxItem CreateInboxItem(Guid id)
    {
        return new OpportunityInboxItem
        {
            Id = id,
            Marketplace = Marketplace.Workana,
            IntakeChannel = IntakeChannel.Manual,
            Title = "Test",
            BudgetKind = BudgetKind.Range.ToString(),
            BudgetBasis = BudgetBasis.FixedProject.ToString(),
            CapturedAt = DateTimeOffset.UtcNow,
            Status = OpportunityStatus.New
        };
    }
}

using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application.Tests;

public class ImportOpportunitiesHandlerTests
{
    private readonly FakeOpportunityRepository _opportunityRepository = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private ImportOpportunitiesHandler CreateHandler()
    {
        var createHandler = new CreateManualOpportunityHandler(_opportunityRepository, _timeProvider);
        return new ImportOpportunitiesHandler(createHandler);
    }

    private static CreateManualOpportunityRequest CreateValidRequest(string title = "Test Opportunity")
    {
        return new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = title,
            Description = "Build a REST API",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Range,
                Basis = ContractBudgetBasis.FixedProject,
                Minimum = 5000,
                Maximum = 15000,
                Currency = "BRL"
            },
            RequiredSkills = ["C#", "API"]
        };
    }

    [Fact]
    public async Task SingleValidItem_ImportsSuccessfully()
    {
        var handler = CreateHandler();
        var request = new ImportOpportunitiesRequest
        {
            Items = [CreateValidRequest()]
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.Empty(result.Errors);
        Assert.Single(_opportunityRepository.AddedOpportunities);
    }

    [Fact]
    public async Task MultipleValidItems_ImportsAll()
    {
        var handler = CreateHandler();
        var request = new ImportOpportunitiesRequest
        {
            Items =
            [
                CreateValidRequest("First"),
                CreateValidRequest("Second"),
                CreateValidRequest("Third")
            ]
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(3, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.Empty(result.Errors);
        Assert.Equal(3, _opportunityRepository.AddedOpportunities.Count);
    }

    [Fact]
    public async Task EmptyList_ReturnsZeroImported()
    {
        var handler = CreateHandler();
        var request = new ImportOpportunitiesRequest { Items = [] };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(0, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ItemWithNullBudget_StillImports()
    {
        var handler = CreateHandler();
        var request = new ImportOpportunitiesRequest
        {
            Items =
            [
                new CreateManualOpportunityRequest
                {
                    Marketplace = ContractMarketplace.Upwork,
                    Title = "No Budget",
                    Description = "Test"
                }
            ]
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async Task PartialFailure_ReportsBothSuccessAndErrors()
    {
        var handler = CreateHandler();
        var request = new ImportOpportunitiesRequest
        {
            Items =
            [
                CreateValidRequest("Valid One"),
                new CreateManualOpportunityRequest
                {
                    Marketplace = ContractMarketplace.Workana,
                    Title = "",
                    Description = ""
                },
                CreateValidRequest("Valid Two")
            ]
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(2, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
        Assert.Equal(1, result.Errors[0].Index);
        Assert.Equal("Valid Two", _opportunityRepository.AddedOpportunities[1].Title);
    }

    [Fact]
    public async Task AllItemsFail_ReportsAllErrors()
    {
        var handler = CreateHandler();
        var request = new ImportOpportunitiesRequest
        {
            Items =
            [
                new CreateManualOpportunityRequest
                {
                    Marketplace = ContractMarketplace.Workana,
                    Title = "",
                    Description = ""
                },
                new CreateManualOpportunityRequest
                {
                    Marketplace = ContractMarketplace.Workana,
                    Title = "",
                    Description = ""
                }
            ]
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(0, result.Imported);
        Assert.Equal(2, result.Failed);
        Assert.Equal(2, result.Errors.Count);
    }
}

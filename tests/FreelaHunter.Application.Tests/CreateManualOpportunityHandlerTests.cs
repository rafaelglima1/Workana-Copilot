using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Application.Tests;

public class CreateManualOpportunityHandlerTests
{
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly FakeOpportunityRepository _repository = new();
    private readonly CreateManualOpportunityHandler _handler;

    public CreateManualOpportunityHandlerTests()
    {
        _handler = new CreateManualOpportunityHandler(_repository, _timeProvider);
    }

    [Fact]
    public async Task HandleAsync_CreatesValidOpportunity()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test Project",
            Description = "A test project",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Amount = 1000m,
                Currency = "BRL"
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(ContractMarketplace.Workana, result.Marketplace);
        Assert.Equal(ContractIntakeChannel.Manual, result.IntakeChannel);
        Assert.Equal("Test Project", result.Title);
        Assert.Equal(ContractOpportunityStatus.New, result.Status);
        Assert.Single(_repository.AddedOpportunities);
    }

    [Fact]
    public async Task HandleAsync_ForcesIntakeChannelManual()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Upwork,
            Title = "Test",
            Description = "Description"
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(ContractIntakeChannel.Manual, result.IntakeChannel);
    }

    [Fact]
    public async Task HandleAsync_UsesTimeProvider()
    {
        var fixedTime = new DateTimeOffset(2025, 3, 15, 8, 30, 0, TimeSpan.Zero);
        _timeProvider.SetUtcNow(fixedTime);

        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description"
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(fixedTime, result.CapturedAt);
    }

    [Fact]
    public async Task HandleAsync_NormalizesTitle()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "  Test Project  ",
            Description = "Description"
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal("Test Project", result.Title);
    }

    [Fact]
    public async Task HandleAsync_PersistsInRepository()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description"
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Single(_repository.AddedOpportunities);
        Assert.Equal(result.Id, _repository.AddedOpportunities[0].Id.Value);
    }

    [Fact]
    public async Task HandleAsync_NormalizesSkills()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            RequiredSkills = ["  C#  ", "c#", "Blazor"]
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(2, result.RequiredSkills.Count);
        Assert.Contains("C#", result.RequiredSkills);
        Assert.Contains("Blazor", result.RequiredSkills);
    }

    [Fact]
    public async Task HandleAsync_BudgetExact_Valid()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Amount = 500m,
                Currency = "USD"
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(ContractBudgetKind.Exact, result.Budget.Kind);
        Assert.Equal(500m, result.Budget.Amount);
        Assert.Equal("USD", result.Budget.Currency);
    }

    [Fact]
    public async Task HandleAsync_BudgetRange_Valid()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Range,
                Minimum = 500m,
                Maximum = 2000m,
                Currency = "EUR"
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(ContractBudgetKind.Range, result.Budget.Kind);
        Assert.Equal(500m, result.Budget.Minimum);
        Assert.Equal(2000m, result.Budget.Maximum);
        Assert.Equal("EUR", result.Budget.Currency);
    }

    [Fact]
    public async Task HandleAsync_BudgetUpTo_Valid()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.UpTo,
                Maximum = 3000m,
                Currency = "GBP"
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(ContractBudgetKind.UpTo, result.Budget.Kind);
        Assert.Equal(3000m, result.Budget.Maximum);
    }

    [Fact]
    public async Task HandleAsync_BudgetFrom_Valid()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.From,
                Minimum = 1000m,
                Currency = "BRL"
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(ContractBudgetKind.From, result.Budget.Kind);
        Assert.Equal(1000m, result.Budget.Minimum);
    }

    [Fact]
    public async Task HandleAsync_BudgetNegotiable_WithCurrency()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Negotiable,
                Currency = "USD"
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(ContractBudgetKind.Negotiable, result.Budget.Kind);
        Assert.Equal("USD", result.Budget.Currency);
    }

    [Fact]
    public async Task HandleAsync_BudgetUnknown_ReturnsUnknown()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput { Kind = ContractBudgetKind.Unknown }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(ContractBudgetKind.Unknown, result.Budget.Kind);
    }

    [Fact]
    public async Task HandleAsync_BudgetExact_MissingAmount_ThrowsDomainException()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Currency = "BRL"
            }
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_BudgetExact_MissingCurrency_ThrowsDomainException()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Budget = new BudgetInput
            {
                Kind = ContractBudgetKind.Exact,
                Amount = 100m
            }
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ClientSnapshot_Valid()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Client = new ClientInput
            {
                DisplayName = "John",
                Country = "BR",
                Rating = 4.5m
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal("John", result.Client.DisplayName);
        Assert.Equal("BR", result.Client.Country);
        Assert.Equal(4.5m, result.Client.Rating);
    }

    [Fact]
    public async Task HandleAsync_CompetitionSnapshot_Valid()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Test",
            Description = "Description",
            Competition = new CompetitionInput
            {
                ProposalCount = 5,
                InterestedFreelancerCount = 12
            }
        };

        var result = await _handler.HandleAsync(request, CancellationToken.None);

        Assert.Equal(5, result.Competition.ProposalCount);
        Assert.Equal(12, result.Competition.InterestedFreelancerCount);
    }

    [Fact]
    public async Task HandleAsync_EmptyTitle_ThrowsDomainException()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "",
            Description = "Description"
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_EmptyDescription_ThrowsDomainException()
    {
        var request = new CreateManualOpportunityRequest
        {
            Marketplace = ContractMarketplace.Workana,
            Title = "Title",
            Description = ""
        };

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(request, CancellationToken.None));
    }
}

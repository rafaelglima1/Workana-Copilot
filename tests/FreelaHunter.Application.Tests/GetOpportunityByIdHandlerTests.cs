using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application.Tests;

public class GetOpportunityByIdHandlerTests
{
    private readonly FakeOpportunityRepository _repository = new();
    private readonly GetOpportunityByIdHandler _handler;

    public GetOpportunityByIdHandlerTests()
    {
        _handler = new GetOpportunityByIdHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_ExistingOpportunity_ReturnsResponse()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test",
            description: "Description",
            budget: Budget.Unknown(),
            client: ClientSnapshot.Create(),
            competition: CompetitionSnapshot.Create(),
            capturedAt: DateTimeOffset.UtcNow);

        await _repository.AddAsync(opportunity, CancellationToken.None);

        var result = await _handler.HandleAsync(opportunity.Id.Value, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(opportunity.Id.Value, result.Id);
        Assert.Equal("Test", result.Title);
    }

    [Fact]
    public async Task HandleAsync_NonExistingOpportunity_ReturnsNull()
    {
        var result = await _handler.HandleAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_EmptyGuid_ThrowsDomainException()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.HandleAsync(Guid.Empty, CancellationToken.None));
    }
}

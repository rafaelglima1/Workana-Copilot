using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.Tests;

public class CompetitionSnapshotTests
{
    [Fact]
    public void Create_WithValidValues_ReturnsSnapshot()
    {
        var competition = CompetitionSnapshot.Create(
            proposalCount: 5,
            interestedFreelancerCount: 12);

        Assert.Equal(5, competition.ProposalCount);
        Assert.Equal(12, competition.InterestedFreelancerCount);
    }

    [Fact]
    public void Create_NullValues_AreAccepted()
    {
        var competition = CompetitionSnapshot.Create();
        Assert.Null(competition.ProposalCount);
        Assert.Null(competition.InterestedFreelancerCount);
    }

    [Fact]
    public void Create_ZeroValues_AreValid()
    {
        var competition = CompetitionSnapshot.Create(
            proposalCount: 0,
            interestedFreelancerCount: 0);

        Assert.Equal(0, competition.ProposalCount);
        Assert.Equal(0, competition.InterestedFreelancerCount);
    }

    [Fact]
    public void Create_NegativeProposalCount_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CompetitionSnapshot.Create(proposalCount: -1));
        Assert.Contains("ProposalCount", ex.Message);
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_NegativeInterestedFreelancerCount_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CompetitionSnapshot.Create(interestedFreelancerCount: -1));
        Assert.Contains("InterestedFreelancerCount", ex.Message);
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var c1 = CompetitionSnapshot.Create(proposalCount: 5);
        var c2 = CompetitionSnapshot.Create(proposalCount: 5);
        Assert.Equal(c1, c2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var c1 = CompetitionSnapshot.Create(proposalCount: 5);
        var c2 = CompetitionSnapshot.Create(proposalCount: 10);
        Assert.NotEqual(c1, c2);
    }
}

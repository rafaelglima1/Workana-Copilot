using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record CompetitionSnapshot
{
    public int? ProposalCount { get; }
    public int? InterestedFreelancerCount { get; }

    private CompetitionSnapshot(int? proposalCount, int? interestedFreelancerCount)
    {
        ProposalCount = proposalCount;
        InterestedFreelancerCount = interestedFreelancerCount;
    }

    public static CompetitionSnapshot Create(
        int? proposalCount = null,
        int? interestedFreelancerCount = null)
    {
        if (proposalCount is < 0)
            throw new DomainException("ProposalCount cannot be negative.");

        if (interestedFreelancerCount is < 0)
            throw new DomainException("InterestedFreelancerCount cannot be negative.");

        return new CompetitionSnapshot(proposalCount, interestedFreelancerCount);
    }
}

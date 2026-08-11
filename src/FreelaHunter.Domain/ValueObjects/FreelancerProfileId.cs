using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record FreelancerProfileId
{
    public Guid Value { get; }

    private FreelancerProfileId(Guid value)
    {
        Value = value;
    }

    public static FreelancerProfileId New() => new(Guid.NewGuid());

    public static FreelancerProfileId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("FreelancerProfileId cannot be empty.");

        return new FreelancerProfileId(value);
    }
}

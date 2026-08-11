namespace FreelaHunter.Domain.ValueObjects;

public sealed record OpportunityId
{
    public Guid Value { get; }

    private OpportunityId(Guid value)
    {
        Value = value;
    }

    public static OpportunityId New() => new(Guid.NewGuid());

    public static OpportunityId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new Exceptions.DomainException("OpportunityId cannot be empty.");

        return new OpportunityId(value);
    }
}

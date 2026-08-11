namespace FreelaHunter.Domain.ValueObjects;

public sealed record CurrencyCode
{
    public string Value { get; }

    private CurrencyCode(string value)
    {
        Value = value;
    }

    public static CurrencyCode From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new Exceptions.DomainException("CurrencyCode cannot be null or empty.");

        var normalized = value.Trim().ToUpperInvariant();

        if (normalized.Length != 3)
            throw new Exceptions.DomainException("CurrencyCode must be exactly 3 characters.");

        if (!normalized.All(char.IsLetter))
            throw new Exceptions.DomainException("CurrencyCode must contain only letters.");

        return new CurrencyCode(normalized);
    }
}

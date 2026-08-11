using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record Budget
{
    public BudgetKind Kind { get; }
    public decimal? Amount { get; }
    public decimal? Minimum { get; }
    public decimal? Maximum { get; }
    public CurrencyCode? Currency { get; }

    private Budget(BudgetKind kind, decimal? amount, decimal? minimum, decimal? maximum, CurrencyCode? currency)
    {
        Kind = kind;
        Amount = amount;
        Minimum = minimum;
        Maximum = maximum;
        Currency = currency;
    }

    public static Budget Unknown() =>
        new(BudgetKind.Unknown, null, null, null, null);

    public static Budget Negotiable(CurrencyCode? currency = null) =>
        new(BudgetKind.Negotiable, null, null, null, currency);

    public static Budget Exact(decimal amount, CurrencyCode currency)
    {
        if (amount < 0)
            throw new DomainException("Budget amount cannot be negative.");

        return new(BudgetKind.Exact, amount, null, null, currency);
    }

    public static Budget Range(decimal minimum, decimal maximum, CurrencyCode currency)
    {
        if (minimum < 0)
            throw new DomainException("Budget minimum cannot be negative.");

        if (maximum < 0)
            throw new DomainException("Budget maximum cannot be negative.");

        if (minimum > maximum)
            throw new DomainException("Budget minimum cannot be greater than maximum.");

        return new(BudgetKind.Range, null, minimum, maximum, currency);
    }

    public static Budget UpTo(decimal maximum, CurrencyCode currency)
    {
        if (maximum < 0)
            throw new DomainException("Budget maximum cannot be negative.");

        return new(BudgetKind.UpTo, null, null, maximum, currency);
    }

    public static Budget From(decimal minimum, CurrencyCode currency)
    {
        if (minimum < 0)
            throw new DomainException("Budget minimum cannot be negative.");

        return new(BudgetKind.From, null, minimum, null, currency);
    }
}

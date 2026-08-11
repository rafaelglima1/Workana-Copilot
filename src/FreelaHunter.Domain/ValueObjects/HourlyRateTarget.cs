using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record HourlyRateTarget
{
    public decimal Amount { get; }
    public CurrencyCode Currency { get; }

    private HourlyRateTarget(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static HourlyRateTarget Create(decimal amount, CurrencyCode currency)
    {
        if (amount <= 0)
            throw new DomainException("HourlyRateTarget amount must be greater than 0.");

        return new HourlyRateTarget(amount, currency);
    }
}

using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record ScoreValue : IComparable<ScoreValue>
{
    public decimal Value { get; }

    private ScoreValue(decimal value)
    {
        Value = value;
    }

    public static ScoreValue From(decimal value)
    {
        if (value < 0 || value > 100)
            throw new DomainException($"ScoreValue must be between 0 and 100. Got {value}.");

        var rounded = Math.Round(value, 2, MidpointRounding.AwayFromZero);
        return new ScoreValue(rounded);
    }

    public int CompareTo(ScoreValue? other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    public static bool operator >(ScoreValue left, ScoreValue right) => left.Value > right.Value;
    public static bool operator <(ScoreValue left, ScoreValue right) => left.Value < right.Value;
    public static bool operator >=(ScoreValue left, ScoreValue right) => left.Value >= right.Value;
    public static bool operator <=(ScoreValue left, ScoreValue right) => left.Value <= right.Value;

    public static bool operator >(ScoreValue left, decimal right) => left.Value > right;
    public static bool operator <(ScoreValue left, decimal right) => left.Value < right;
    public static bool operator >=(ScoreValue left, decimal right) => left.Value >= right;
    public static bool operator <=(ScoreValue left, decimal right) => left.Value <= right;

    public static bool operator >(ScoreValue left, int right) => left.Value > right;
    public static bool operator <(ScoreValue left, int right) => left.Value < right;
    public static bool operator >=(ScoreValue left, int right) => left.Value >= right;
    public static bool operator <=(ScoreValue left, int right) => left.Value <= right;

    public override string ToString() => Value.ToString("G");
}

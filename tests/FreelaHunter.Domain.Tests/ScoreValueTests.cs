using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class ScoreValueTests
{
    [Fact]
    public void From_Zero_ReturnsZero()
    {
        var score = ScoreValue.From(0);
        Assert.Equal(0, score.Value);
    }

    [Fact]
    public void From_100_Returns100()
    {
        var score = ScoreValue.From(100);
        Assert.Equal(100, score.Value);
    }

    [Fact]
    public void From_Negative_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ScoreValue.From(-1));
    }

    [Fact]
    public void From_GreaterThan100_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ScoreValue.From(101));
    }

    [Fact]
    public void From_RoundsToTwoDecimalPlaces()
    {
        var score = ScoreValue.From(33.333m);
        Assert.Equal(33.33m, score.Value);
    }

    [Fact]
    public void EqualValues_AreEqual()
    {
        var a = ScoreValue.From(50);
        var b = ScoreValue.From(50);
        Assert.Equal(a, b);
    }

    [Fact]
    public void DifferentValues_AreNotEqual()
    {
        var a = ScoreValue.From(50);
        var b = ScoreValue.From(60);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ComparisonOperators_Work()
    {
        var low = ScoreValue.From(30);
        var high = ScoreValue.From(70);

        Assert.True(high > low);
        Assert.True(low < high);
        Assert.True(high >= low);
        Assert.True(low <= high);
        Assert.True(low >= 30);
        Assert.True(low <= 30);
    }
}

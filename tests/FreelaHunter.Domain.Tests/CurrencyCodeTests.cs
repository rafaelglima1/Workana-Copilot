using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.Tests;

public class CurrencyCodeTests
{
    [Theory]
    [InlineData("BRL")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public void From_ValidCode_ReturnsCode(string code)
    {
        var currency = CurrencyCode.From(code);
        Assert.Equal(code.ToUpperInvariant(), currency.Value);
    }

    [Fact]
    public void From_Lowercase_NormalizesToUppercase()
    {
        var currency = CurrencyCode.From("usd");
        Assert.Equal("USD", currency.Value);
    }

    [Fact]
    public void From_WithSpaces_TrimsAndNormalizes()
    {
        var currency = CurrencyCode.From("  eur  ");
        Assert.Equal("EUR", currency.Value);
    }

    [Fact]
    public void From_Null_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CurrencyCode.From(null));
    }

    [Fact]
    public void From_Empty_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CurrencyCode.From(""));
    }

    [Fact]
    public void From_Whitespace_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => CurrencyCode.From("   "));
    }

    [Fact]
    public void From_TwoCharacters_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => CurrencyCode.From("US"));
        Assert.Contains("3 characters", ex.Message);
    }

    [Fact]
    public void From_FourCharacters_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => CurrencyCode.From("USDL"));
        Assert.Contains("3 characters", ex.Message);
    }

    [Fact]
    public void From_NonAlpha_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => CurrencyCode.From("US1"));
        Assert.Contains("letters", ex.Message);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var c1 = CurrencyCode.From("USD");
        var c2 = CurrencyCode.From("USD");
        Assert.Equal(c1, c2);
    }

    [Fact]
    public void Equality_DifferentValue_AreNotEqual()
    {
        var c1 = CurrencyCode.From("USD");
        var c2 = CurrencyCode.From("EUR");
        Assert.NotEqual(c1, c2);
    }
}

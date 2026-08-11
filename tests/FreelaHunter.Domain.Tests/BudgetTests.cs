using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.Tests;

public class BudgetTests
{
    [Fact]
    public void Unknown_ReturnsCorrectKind()
    {
        var budget = Budget.Unknown();
        Assert.Equal(BudgetKind.Unknown, budget.Kind);
        Assert.Null(budget.Amount);
        Assert.Null(budget.Minimum);
        Assert.Null(budget.Maximum);
        Assert.Null(budget.Currency);
    }

    [Fact]
    public void Negotiable_WithoutCurrency_ReturnsCorrectKind()
    {
        var budget = Budget.Negotiable();
        Assert.Equal(BudgetKind.Negotiable, budget.Kind);
        Assert.Null(budget.Currency);
    }

    [Fact]
    public void Negotiable_WithCurrency_ReturnsCorrectKind()
    {
        var currency = CurrencyCode.From("USD");
        var budget = Budget.Negotiable(currency);
        Assert.Equal(BudgetKind.Negotiable, budget.Kind);
        Assert.Equal(currency, budget.Currency);
    }

    [Fact]
    public void Exact_ValidAmount_ReturnsCorrectValues()
    {
        var currency = CurrencyCode.From("BRL");
        var budget = Budget.Exact(1000m, currency);
        Assert.Equal(BudgetKind.Exact, budget.Kind);
        Assert.Equal(1000m, budget.Amount);
        Assert.Equal(currency, budget.Currency);
    }

    [Fact]
    public void Exact_ZeroAmount_IsValid()
    {
        var currency = CurrencyCode.From("USD");
        var budget = Budget.Exact(0m, currency);
        Assert.Equal(0m, budget.Amount);
    }

    [Fact]
    public void Exact_NegativeAmount_ThrowsDomainException()
    {
        var currency = CurrencyCode.From("USD");
        var ex = Assert.Throws<DomainException>(() => Budget.Exact(-1m, currency));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Range_Valid_ReturnsCorrectValues()
    {
        var currency = CurrencyCode.From("EUR");
        var budget = Budget.Range(500m, 2000m, currency);
        Assert.Equal(BudgetKind.Range, budget.Kind);
        Assert.Equal(500m, budget.Minimum);
        Assert.Equal(2000m, budget.Maximum);
        Assert.Equal(currency, budget.Currency);
    }

    [Fact]
    public void Range_EqualMinAndMax_IsValid()
    {
        var currency = CurrencyCode.From("USD");
        var budget = Budget.Range(1000m, 1000m, currency);
        Assert.Equal(1000m, budget.Minimum);
        Assert.Equal(1000m, budget.Maximum);
    }

    [Fact]
    public void Range_MinGreaterThanMax_ThrowsDomainException()
    {
        var currency = CurrencyCode.From("USD");
        var ex = Assert.Throws<DomainException>(() => Budget.Range(2000m, 500m, currency));
        Assert.Contains("minimum", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("maximum", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Range_NegativeMin_ThrowsDomainException()
    {
        var currency = CurrencyCode.From("USD");
        var ex = Assert.Throws<DomainException>(() => Budget.Range(-1m, 100m, currency));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Range_NegativeMax_ThrowsDomainException()
    {
        var currency = CurrencyCode.From("USD");
        var ex = Assert.Throws<DomainException>(() => Budget.Range(0m, -1m, currency));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpTo_Valid_ReturnsCorrectValues()
    {
        var currency = CurrencyCode.From("GBP");
        var budget = Budget.UpTo(5000m, currency);
        Assert.Equal(BudgetKind.UpTo, budget.Kind);
        Assert.Equal(5000m, budget.Maximum);
        Assert.Equal(currency, budget.Currency);
    }

    [Fact]
    public void UpTo_NegativeMaximum_ThrowsDomainException()
    {
        var currency = CurrencyCode.From("USD");
        var ex = Assert.Throws<DomainException>(() => Budget.UpTo(-1m, currency));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void From_Valid_ReturnsCorrectValues()
    {
        var currency = CurrencyCode.From("BRL");
        var budget = Budget.From(1000m, currency);
        Assert.Equal(BudgetKind.From, budget.Kind);
        Assert.Equal(1000m, budget.Minimum);
        Assert.Equal(currency, budget.Currency);
    }

    [Fact]
    public void From_NegativeMinimum_ThrowsDomainException()
    {
        var currency = CurrencyCode.From("USD");
        var ex = Assert.Throws<DomainException>(() => Budget.From(-1m, currency));
        Assert.Contains("negative", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var currency = CurrencyCode.From("USD");
        var b1 = Budget.Exact(100m, currency);
        var b2 = Budget.Exact(100m, currency);
        Assert.Equal(b1, b2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var currency = CurrencyCode.From("USD");
        var b1 = Budget.Exact(100m, currency);
        var b2 = Budget.Exact(200m, currency);
        Assert.NotEqual(b1, b2);
    }

    [Fact]
    public void Equality_DifferentKinds_AreNotEqual()
    {
        var currency = CurrencyCode.From("USD");
        var b1 = Budget.Exact(100m, currency);
        var b2 = Budget.UpTo(100m, currency);
        Assert.NotEqual(b1, b2);
    }
}

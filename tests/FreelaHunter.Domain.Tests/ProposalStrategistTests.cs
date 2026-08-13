using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Services;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class ProposalStrategistTests
{
    private static FreelancerProfile CreateProfile(
        decimal? weeklyCapacityHours = 20m,
        decimal hourlyRate = 100m,
        string currency = "BRL")
    {
        return FreelancerProfile.Create(
            [SkillCapability.Create("C#", SkillLevel.Expert)],
            [HourlyRateTarget.Create(hourlyRate, CurrencyCode.From(currency))],
            CurrencyCode.From(currency),
            weeklyCapacityHours);
    }

    private static OpportunityScore CreateScore(decimal overall)
    {
        var scoreValue = ScoreValue.From(overall);
        var technicalFit = ScoreComponent.Create(scoreValue, 30, []);
        var economic = ScoreComponent.Create(scoreValue, 25, []);
        var client = ScoreComponent.Create(scoreValue, 15, []);
        var competition = ScoreComponent.Create(scoreValue, 10, []);
        var delivery = ScoreComponent.Create(scoreValue, 10, []);
        var ai = ScoreComponent.Create(scoreValue, 10, []);

        return OpportunityScore.Create(scoreValue, technicalFit, economic, client, competition, delivery, ai);
    }

    [Fact]
    public void Calculate_ScoreAbove65_ReturnsGo()
    {
        var score = CreateScore(80);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(ProposalDecision.Go, result.Decision);
    }

    [Fact]
    public void Calculate_ScoreBetween50And65_ReturnsMaybe()
    {
        var score = CreateScore(55);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(ProposalDecision.Maybe, result.Decision);
    }

    [Fact]
    public void Calculate_ScoreBelow50_ReturnsNoGo()
    {
        var score = CreateScore(30);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(ProposalDecision.NoGo, result.Decision);
    }

    [Fact]
    public void Calculate_Score80Plus_AdjustsRateUp()
    {
        var score = CreateScore(85);
        var profile = CreateProfile(hourlyRate: 100m);
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(110m, result.RecommendedHourlyRate);
    }

    [Fact]
    public void Calculate_ScoreBelow65_AdjustsRateDown()
    {
        var score = CreateScore(55);
        var profile = CreateProfile(hourlyRate: 100m);
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(90m, result.RecommendedHourlyRate);
    }

    [Fact]
    public void Calculate_ScoreBetween65And80_KeepsOriginalRate()
    {
        var score = CreateScore(70);
        var profile = CreateProfile(hourlyRate: 100m);
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(100m, result.RecommendedHourlyRate);
    }

    [Fact]
    public void Calculate_WithCapacity_UsesCapacityForDelivery()
    {
        var score = CreateScore(80);
        var profile = CreateProfile(weeklyCapacityHours: 20m);
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 40m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(14, result.EstimatedDeliveryDays);
    }

    [Fact]
    public void Calculate_WithoutCapacity_UsesDefault8hPerDay()
    {
        var score = CreateScore(80);
        var profile = CreateProfile(weeklyCapacityHours: null);
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 40m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(5, result.EstimatedDeliveryDays);
    }

    [Fact]
    public void Calculate_Revenue_EqualsRateTimesHours()
    {
        var score = CreateScore(70);
        var profile = CreateProfile(hourlyRate: 100m);
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(2000m, result.EstimatedRevenue);
    }

    [Fact]
    public void Calculate_Confidence_Between0And100()
    {
        var score = CreateScore(70);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.InRange(result.Confidence.Value, 0, 100);
    }

    [Fact]
    public void Calculate_Rationale_ContainsDecisionReason()
    {
        var score = CreateScore(80);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Contains(result.Rationale, r => r.Contains("Overall score"));
    }

    [Fact]
    public void Calculate_BudgetUnknown_DoesNotAdjustRate()
    {
        var score = CreateScore(85);
        var profile = CreateProfile(hourlyRate: 100m);
        var budget = Budget.Unknown();

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(100m, result.RecommendedHourlyRate);
    }

    [Fact]
    public void Calculate_BudgetNegotiable_DoesNotAdjustRate()
    {
        var score = CreateScore(85);
        var profile = CreateProfile(hourlyRate: 100m);
        var budget = Budget.Negotiable(CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(100m, result.RecommendedHourlyRate);
    }

    [Fact]
    public void Calculate_DeliveryRiskLow_AddsDeliveryInfo()
    {
        var score = CreateScore(70);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.Low, profile, budget);

        Assert.DoesNotContain(result.Rationale, r => r.Contains("Delivery risk"));
    }

    [Fact]
    public void Calculate_DeliveryRiskHigh_AddsDeliveryRiskToRationale()
    {
        var score = CreateScore(70);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 20m, DeliveryRiskLevel.High, profile, budget);

        Assert.Contains(result.Rationale, r => r.Contains("Delivery risk"));
    }

    [Fact]
    public void Calculate_EstimatedHoursZero_ReturnsZeroDelivery()
    {
        var score = CreateScore(80);
        var profile = CreateProfile();
        var budget = Budget.Exact(5000, CurrencyCode.From("BRL"));

        var result = ProposalStrategist.Calculate(score, 0m, DeliveryRiskLevel.Low, profile, budget);

        Assert.Equal(0, result.EstimatedDeliveryDays);
        Assert.Equal(0m, result.EstimatedRevenue);
    }
}

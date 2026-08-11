namespace FreelaHunter.Domain.ValueObjects;

public sealed record ScoreComponent
{
    public ScoreValue Score { get; }
    public decimal Weight { get; }
    public decimal WeightedContribution { get; }
    public IReadOnlyCollection<ScoringRuleCode> Rules { get; }

    private ScoreComponent(
        ScoreValue score,
        decimal weight,
        decimal weightedContribution,
        IReadOnlyCollection<ScoringRuleCode> rules)
    {
        Score = score;
        Weight = weight;
        WeightedContribution = weightedContribution;
        Rules = rules;
    }

    public static ScoreComponent Create(
        ScoreValue score,
        decimal weight,
        IEnumerable<ScoringRuleCode> rules)
    {
        var weightedContribution = Math.Round(score.Value * weight / 100, 2, MidpointRounding.AwayFromZero);
        var ruleList = rules.ToList().AsReadOnly();
        return new ScoreComponent(score, weight, weightedContribution, ruleList);
    }
}

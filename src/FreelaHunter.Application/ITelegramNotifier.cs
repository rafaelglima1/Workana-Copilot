namespace FreelaHunter.Application;

public interface ITelegramNotifier
{
    Task SendOpportunityAlertAsync(TelegramOpportunityAlert alert, CancellationToken cancellationToken);
    Task SendMessageAsync(string message, CancellationToken cancellationToken);
}

public sealed class TelegramOpportunityAlert
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? Marketplace { get; init; }
    public string? BudgetSummary { get; init; }
    public decimal? Score { get; init; }
    public string? Recommendation { get; init; }
    public string? Url { get; init; }
    public IReadOnlyCollection<string> Skills { get; init; } = [];
}

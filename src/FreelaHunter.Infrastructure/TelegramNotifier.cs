using FreelaHunter.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace FreelaHunter.Infrastructure;

public sealed class TelegramNotifier : ITelegramNotifier
{
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotifier> _logger;
    private ITelegramBotClient? _client;

    public TelegramNotifier(
        IOptions<TelegramOptions> options,
        ILogger<TelegramNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOpportunityAlertAsync(
        TelegramOpportunityAlert alert,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogDebug("Telegram disabled or not configured, skipping alert.");
            return;
        }

        var message = FormatOpportunityMessage(alert);
        await SendMessageInternalAsync(message, cancellationToken);
    }

    public async Task SendMessageAsync(string message, CancellationToken cancellationToken)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _logger.LogDebug("Telegram disabled or not configured, skipping message.");
            return;
        }

        await SendMessageInternalAsync(message, cancellationToken);
    }

    private async Task SendMessageInternalAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            _client ??= new TelegramBotClient(_options.BotToken);

            await _client.SendMessage(
                chatId: _options.ChatId,
                text: message,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Telegram message sent to chat {ChatId}", _options.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Telegram message.");
        }
    }

    private static string FormatOpportunityMessage(TelegramOpportunityAlert alert)
    {
        var emoji = alert.Recommendation?.ToLowerInvariant() switch
        {
            "go" => "🟢",
            "apply" => "🔵",
            "maybe" => "🟡",
            _ => "⚪"
        };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{emoji} <b>Nova oportunidade - {alert.Marketplace}</b>");
        sb.AppendLine();
        sb.AppendLine($"<b>{alert.Title}</b>");

        if (!string.IsNullOrWhiteSpace(alert.Description))
        {
            var desc = alert.Description.Length > 200
                ? alert.Description[..200] + "..."
                : alert.Description;
            sb.AppendLine(desc);
        }

        if (!string.IsNullOrWhiteSpace(alert.BudgetSummary))
            sb.AppendLine($"\n💰 {alert.BudgetSummary}");

        if (alert.Score.HasValue)
            sb.AppendLine($"📊 Score: {alert.Score:F1}");

        if (!string.IsNullOrWhiteSpace(alert.Recommendation))
            sb.AppendLine($"📋 Recomendação: {alert.Recommendation.ToUpper()}");

        if (alert.Skills.Count > 0)
            sb.AppendLine($"\n🏷️ {string.Join(", ", alert.Skills.Take(5))}");

        if (!string.IsNullOrWhiteSpace(alert.Url))
            sb.AppendLine($"\n🔗 <a href=\"{alert.Url}\">Ver no Workana</a>");

        return sb.ToString();
    }
}

namespace FreelaHunter.Infrastructure;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;
    public long ChatId { get; init; }
    public bool Enabled { get; init; } = true;
}

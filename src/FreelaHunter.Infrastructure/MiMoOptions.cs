namespace FreelaHunter.Infrastructure;

public sealed class MiMoOptions
{
    public const string SectionName = "MiMo";

    public string BaseUrl { get; init; } = "https://api.xiaomimimo.com/v1";
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "mimo-v2.5";
    public int TimeoutSeconds { get; init; } = 60;
    public int MaxCompletionTokens { get; init; } = 4096;
}

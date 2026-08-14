namespace FreelaHunter.Infrastructure;

public sealed class WorkanaScraperOptions
{
    public const string SectionName = "WorkanaScraper";

    public string BaseUrl { get; init; } = "https://www.workana.com/jobs";
    public string? Technology { get; init; }
    public int MaxPages { get; init; } = 3;
    public int PageDelayMs { get; init; } = 2000;
    public bool Headless { get; init; } = true;
}

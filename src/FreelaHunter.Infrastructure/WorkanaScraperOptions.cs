namespace FreelaHunter.Infrastructure;

public sealed class WorkanaScraperOptions
{
    public const string SectionName = "WorkanaScraper";

    public string BaseUrl { get; init; } = "https://www.workana.com/jobs";
    public string Language { get; init; } = "pt";
    public string Skills { get; init; } = "";
    public string Technology { get; init; } = "";
    public int MaxPages { get; init; } = 3;
    public int PageDelayMs { get; init; } = 2000;
    public bool Headless { get; init; } = true;
    public bool LoginEnabled { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public bool VisitDetails { get; init; } = true;
    public int DetailDelayMs { get; init; } = 3000;
}

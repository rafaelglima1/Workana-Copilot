namespace FreelaHunter.Worker;

public sealed class WorkerOptions
{
    public int IntervalSeconds { get; set; } = 300;
    public int BatchSize { get; set; } = 10;
    public int ScrapeIntervalMinutes { get; set; } = 60;
    public int ScrapeMaxPages { get; set; } = 3;
}

namespace FreelaHunter.Worker;

public sealed class WorkerOptions
{
    public int IntervalSeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 25;
}

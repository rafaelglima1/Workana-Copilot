namespace FreelaHunter.Infrastructure.Providers;

public sealed class CamoufoxOptions
{
    public const string SectionName = "Camoufox";

    public bool Enabled { get; init; }

    public string ServerUrl { get; init; } = "http://localhost:54321";

    public int ConnectTimeoutSeconds { get; init; } = 30;
}

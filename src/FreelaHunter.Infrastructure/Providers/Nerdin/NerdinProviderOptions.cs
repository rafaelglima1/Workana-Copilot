namespace FreelaHunter.Infrastructure.Providers.Nerdin;

public sealed class NerdinProviderOptions
{
    public const string SectionName = "Providers:Nerdin";

    public string BaseUrl { get; init; } = "https://www.nerdin.com.br/vagas";

    public string Technology { get; init; } = "";

    public string Location { get; init; } = "";

    public int MaxPages { get; init; } = 3;

    public int PageDelayMs { get; init; } = 2000;

    public bool Headless { get; init; } = true;

    public bool LoginEnabled { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }

    public bool VisitDetails { get; init; } = true;

    public int DetailDelayMs { get; init; } = 3000;
}

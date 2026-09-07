namespace FreelaHunter.Application.Providers;

public sealed record JobApplicationData
{
    public required string ProviderId { get; init; }

    public required string ExternalUrl { get; init; }

    public required string OpportunityTitle { get; init; }

    public string? CoverLetter { get; init; }

    public decimal? ProposedRate { get; init; }

    public int? DeliveryDays { get; init; }

    public Dictionary<string, string>? AdditionalFields { get; init; }
}

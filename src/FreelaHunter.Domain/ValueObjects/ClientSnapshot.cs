using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Domain.ValueObjects;

public sealed record ClientSnapshot
{
    public string? DisplayName { get; }
    public string? Country { get; }
    public int? PublishedProjects { get; }
    public int? PaidProjects { get; }
    public decimal? Rating { get; }

    private ClientSnapshot(
        string? displayName,
        string? country,
        int? publishedProjects,
        int? paidProjects,
        decimal? rating)
    {
        DisplayName = displayName;
        Country = country;
        PublishedProjects = publishedProjects;
        PaidProjects = paidProjects;
        Rating = rating;
    }

    public static ClientSnapshot Create(
        string? displayName = null,
        string? country = null,
        int? publishedProjects = null,
        int? paidProjects = null,
        decimal? rating = null)
    {
        displayName = NormalizeString(displayName);
        country = NormalizeString(country);

        if (publishedProjects is < 0)
            throw new DomainException("PublishedProjects cannot be negative.");

        if (paidProjects is < 0)
            throw new DomainException("PaidProjects cannot be negative.");

        if (publishedProjects.HasValue && paidProjects.HasValue && paidProjects > publishedProjects)
            throw new DomainException("PaidProjects cannot be greater than PublishedProjects.");

        if (rating is < 0 or > 5)
            throw new DomainException("Rating must be between 0 and 5.");

        return new ClientSnapshot(displayName, country, publishedProjects, paidProjects, rating);
    }

    private static string? NormalizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}

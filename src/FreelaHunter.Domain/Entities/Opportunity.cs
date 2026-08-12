using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Entities;

public sealed class Opportunity
{
    private readonly List<string> _requiredSkills = [];

    public OpportunityId Id { get; }
    public Marketplace Marketplace { get; }
    public IntakeChannel IntakeChannel { get; }
    public string? ExternalReference { get; }
    public string Title { get; }
    public string Description { get; }
    public string? Category { get; }
    public string? Subcategory { get; }
    public Budget Budget { get; }
    public ClientSnapshot Client { get; }
    public CompetitionSnapshot Competition { get; }
    public IReadOnlyCollection<string> RequiredSkills => _requiredSkills.AsReadOnly();
    public DateTimeOffset? PublishedAt { get; }
    public DateOnly? ClientDeadline { get; }
    public DateTimeOffset CapturedAt { get; }
    public OpportunityStatus Status { get; private set; }

    private Opportunity(
        OpportunityId id,
        Marketplace marketplace,
        IntakeChannel intakeChannel,
        string? externalReference,
        string title,
        string description,
        string? category,
        string? subcategory,
        Budget budget,
        ClientSnapshot client,
        CompetitionSnapshot competition,
        IReadOnlyCollection<string> requiredSkills,
        DateTimeOffset? publishedAt,
        DateOnly? clientDeadline,
        DateTimeOffset capturedAt)
    {
        Id = id;
        Marketplace = marketplace;
        IntakeChannel = intakeChannel;
        ExternalReference = externalReference;
        Title = title;
        Description = description;
        Category = category;
        Subcategory = subcategory;
        Budget = budget;
        Client = client;
        Competition = competition;
        _requiredSkills.AddRange(requiredSkills);
        PublishedAt = publishedAt;
        ClientDeadline = clientDeadline;
        CapturedAt = capturedAt;
        Status = OpportunityStatus.New;
    }

    public static Opportunity Create(
        Marketplace marketplace,
        IntakeChannel intakeChannel,
        string title,
        string description,
        Budget budget,
        ClientSnapshot client,
        CompetitionSnapshot competition,
        DateTimeOffset capturedAt,
        string? externalReference = null,
        string? category = null,
        string? subcategory = null,
        IEnumerable<string>? requiredSkills = null,
        DateTimeOffset? publishedAt = null,
        DateOnly? clientDeadline = null)
    {
        title = NormalizeAndValidateTitle(title);
        description = NormalizeAndValidateDescription(description);
        externalReference = NormalizeOptional(externalReference);
        category = NormalizeOptional(category);
        subcategory = NormalizeOptional(subcategory);
        var skills = NormalizeSkills(requiredSkills);

        return new Opportunity(
            OpportunityId.New(),
            marketplace,
            intakeChannel,
            externalReference,
            title,
            description,
            category,
            subcategory,
            budget,
            client,
            competition,
            skills,
            publishedAt,
            clientDeadline,
            capturedAt);
    }

    public void Shortlist()
    {
        if (Status == OpportunityStatus.Archived)
            throw new DomainException("Cannot shortlist an archived opportunity.");

        Status = OpportunityStatus.Shortlisted;
    }

    public void Ignore()
    {
        if (Status == OpportunityStatus.Archived)
            throw new DomainException("Cannot ignore an archived opportunity.");

        Status = OpportunityStatus.Ignored;
    }

    public void Archive()
    {
        Status = OpportunityStatus.Archived;
    }

    private static string NormalizeAndValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");

        var normalized = title.Trim();

        if (normalized.Length == 0)
            throw new DomainException("Title cannot be empty.");

        if (normalized.Length > 300)
            throw new DomainException("Title cannot exceed 300 characters.");

        return normalized;
    }

    private static string NormalizeAndValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        var normalized = description.Trim();

        if (normalized.Length == 0)
            throw new DomainException("Description cannot be empty.");

        return normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static IReadOnlyCollection<string> NormalizeSkills(IEnumerable<string>? skills)
    {
        if (skills is null)
            return [];

        return skills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    public static Opportunity Rehydrate(
        OpportunityId id,
        Marketplace marketplace,
        IntakeChannel intakeChannel,
        string? externalReference,
        string title,
        string description,
        string? category,
        string? subcategory,
        Budget budget,
        ClientSnapshot client,
        CompetitionSnapshot competition,
        IReadOnlyCollection<string> requiredSkills,
        DateTimeOffset? publishedAt,
        DateOnly? clientDeadline,
        DateTimeOffset capturedAt,
        OpportunityStatus status)
    {
        title = NormalizeAndValidateTitle(title);
        description = NormalizeAndValidateDescription(description);
        externalReference = NormalizeOptional(externalReference);
        category = NormalizeOptional(category);
        subcategory = NormalizeOptional(subcategory);
        var skills = NormalizeSkills(requiredSkills);

        var opportunity = new Opportunity(
            id,
            marketplace,
            intakeChannel,
            externalReference,
            title,
            description,
            category,
            subcategory,
            budget,
            client,
            competition,
            skills,
            publishedAt,
            clientDeadline,
            capturedAt);

        opportunity.Status = status;
        return opportunity;
    }
}

using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Domain.Tests;

public class OpportunityTests
{
    private static readonly DateTimeOffset FixedCapturedAt = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Budget DefaultBudget = Budget.Exact(1000m, CurrencyCode.From("BRL"));
    private static readonly ClientSnapshot DefaultClient = ClientSnapshot.Create(displayName: "Test Client");
    private static readonly CompetitionSnapshot DefaultCompetition = CompetitionSnapshot.Create();

    [Fact]
    public void Create_ValidOpportunity_ReturnsNewOpportunity()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test Project",
            description: "A test project description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt);

        Assert.NotEqual(Guid.Empty, opportunity.Id.Value);
        Assert.Equal(Marketplace.Workana, opportunity.Marketplace);
        Assert.Equal(IntakeChannel.Manual, opportunity.IntakeChannel);
        Assert.Equal("Test Project", opportunity.Title);
        Assert.Equal("A test project description", opportunity.Description);
        Assert.Equal(DefaultBudget, opportunity.Budget);
        Assert.Equal(DefaultClient, opportunity.Client);
        Assert.Equal(DefaultCompetition, opportunity.Competition);
        Assert.Equal(FixedCapturedAt, opportunity.CapturedAt);
        Assert.Equal(OpportunityStatus.New, opportunity.Status);
    }

    [Fact]
    public void CreateWithTitleWhitespace_TrimsTitle()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Upwork,
            intakeChannel: IntakeChannel.Email,
            title: "  Test Project  ",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt);

        Assert.Equal("Test Project", opportunity.Title);
    }

    [Fact]
    public void CreateWithEmptyTitle_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt));

        Assert.Contains("Title", ex.Message);
    }

    [Fact]
    public void CreateWithWhitespaceTitle_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "   ",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt));

        Assert.Contains("Title", ex.Message);
    }

    [Fact]
    public void CreateWithTitleOver300Chars_ThrowsDomainException()
    {
        var longTitle = new string('A', 301);
        var ex = Assert.Throws<DomainException>(() => Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: longTitle,
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt));

        Assert.Contains("300", ex.Message);
    }

    [Fact]
    public void CreateWithTitleExactly300Chars_IsValid()
    {
        var title = new string('A', 300);
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: title,
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt);

        Assert.Equal(300, opportunity.Title.Length);
    }

    [Fact]
    public void CreateWithNullTitle_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: null!,
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt));

        Assert.Contains("Title", ex.Message);
    }

    [Fact]
    public void CreateWithEmptyDescription_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt));

        Assert.Contains("Description", ex.Message);
    }

    [Fact]
    public void CreateWithNullDescription_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: null!,
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt));

        Assert.Contains("Description", ex.Message);
    }

    [Fact]
    public void CreateWithDescriptionWhitespace_TrimsDescription()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "  Description  ",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt);

        Assert.Equal("Description", opportunity.Description);
    }

    [Fact]
    public void Create_CategoryNormalization()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            category: "  Web Development  ",
            subcategory: "  Frontend  ");

        Assert.Equal("Web Development", opportunity.Category);
        Assert.Equal("Frontend", opportunity.Subcategory);
    }

    [Fact]
    public void Create_EmptyCategory_NormalizedToNull()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            category: "",
            subcategory: "   ");

        Assert.Null(opportunity.Category);
        Assert.Null(opportunity.Subcategory);
    }

    [Fact]
    public void Create_ExternalReferenceNormalization()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            externalReference: "  REF-123  ");

        Assert.Equal("REF-123", opportunity.ExternalReference);
    }

    [Fact]
    public void Create_EmptyExternalReference_NormalizedToNull()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            externalReference: "");

        Assert.Null(opportunity.ExternalReference);
    }

    [Fact]
    public void Create_CapturedAt_IsPreserved()
    {
        var capturedAt = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: capturedAt);

        Assert.Equal(capturedAt, opportunity.CapturedAt);
    }

    [Fact]
    public void Create_WithPublishedAt_IsPreserved()
    {
        var publishedAt = new DateTimeOffset(2025, 1, 10, 8, 0, 0, TimeSpan.Zero);
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            publishedAt: publishedAt);

        Assert.Equal(publishedAt, opportunity.PublishedAt);
    }

    [Fact]
    public void Create_WithClientDeadline_IsPreserved()
    {
        var deadline = new DateOnly(2025, 2, 15);
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            clientDeadline: deadline);

        Assert.Equal(deadline, opportunity.ClientDeadline);
    }

    [Fact]
    public void Create_InitialStatusIsNew()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt);

        Assert.Equal(OpportunityStatus.New, opportunity.Status);
    }

    // Status transition tests

    [Fact]
    public void Shortlist_FromNew_SetsShortlisted()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Shortlist();
        Assert.Equal(OpportunityStatus.Shortlisted, opportunity.Status);
    }

    [Fact]
    public void Ignore_FromNew_SetsIgnored()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Ignore();
        Assert.Equal(OpportunityStatus.Ignored, opportunity.Status);
    }

    [Fact]
    public void Archive_FromNew_SetsArchived()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Archive();
        Assert.Equal(OpportunityStatus.Archived, opportunity.Status);
    }

    [Fact]
    public void Archive_FromShortlisted_SetsArchived()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Shortlist();
        opportunity.Archive();
        Assert.Equal(OpportunityStatus.Archived, opportunity.Status);
    }

    [Fact]
    public void Archive_FromIgnored_SetsArchived()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Ignore();
        opportunity.Archive();
        Assert.Equal(OpportunityStatus.Archived, opportunity.Status);
    }

    [Fact]
    public void Shortlist_IsIdempotent()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Shortlist();
        opportunity.Shortlist();
        Assert.Equal(OpportunityStatus.Shortlisted, opportunity.Status);
    }

    [Fact]
    public void Ignore_IsIdempotent()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Ignore();
        opportunity.Ignore();
        Assert.Equal(OpportunityStatus.Ignored, opportunity.Status);
    }

    [Fact]
    public void Archive_IsIdempotent()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Archive();
        opportunity.Archive();
        Assert.Equal(OpportunityStatus.Archived, opportunity.Status);
    }

    [Fact]
    public void Shortlist_FromArchived_ThrowsDomainException()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Archive();
        var ex = Assert.Throws<DomainException>(() => opportunity.Shortlist());
        Assert.Contains("archived", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ignore_FromArchived_ThrowsDomainException()
    {
        var opportunity = CreateDefaultOpportunity();
        opportunity.Archive();
        var ex = Assert.Throws<DomainException>(() => opportunity.Ignore());
        Assert.Contains("archived", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Required skills tests

    [Fact]
    public void Create_WithNullSkills_EmptyCollection()
    {
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            requiredSkills: null);

        Assert.Empty(opportunity.RequiredSkills);
    }

    [Fact]
    public void Create_WithSkills_TrimsAndDeduplicates()
    {
        var skills = new[] { "  C#  ", "c#", "Blazor", "blazor ", "  " };
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            requiredSkills: skills);

        Assert.Equal(2, opportunity.RequiredSkills.Count);
        Assert.Contains("C#", opportunity.RequiredSkills);
        Assert.Contains("Blazor", opportunity.RequiredSkills);
    }

    [Fact]
    public void Create_WithEmptyStringsInSkills_RemovesThem()
    {
        var skills = new[] { "", "   ", "C#" };
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            requiredSkills: skills);

        Assert.Single(opportunity.RequiredSkills);
        Assert.Contains("C#", opportunity.RequiredSkills);
    }

    [Fact]
    public void RequiredSkills_IsReadOnly()
    {
        var skills = new List<string> { "C#", "Blazor" };
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            requiredSkills: skills);

        var collection = opportunity.RequiredSkills;
        Assert.IsAssignableFrom<IReadOnlyCollection<string>>(collection);

        // Verify internal state is not affected by external mutation
        skills.Add("React");
        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void Create_WithEmptySkillsCollection_EmptyResult()
    {
        var skills = Array.Empty<string>();
        var opportunity = Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Title",
            description: "Description",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt,
            requiredSkills: skills);

        Assert.Empty(opportunity.RequiredSkills);
    }

    private static Opportunity CreateDefaultOpportunity()
    {
        return Opportunity.Create(
            marketplace: Marketplace.Workana,
            intakeChannel: IntakeChannel.Manual,
            title: "Test Project",
            description: "A test project",
            budget: DefaultBudget,
            client: DefaultClient,
            competition: DefaultCompetition,
            capturedAt: FixedCapturedAt);
    }
}

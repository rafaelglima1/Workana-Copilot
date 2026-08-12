using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Entities;
using FreelaHunter.Domain.ValueObjects;
using FreelaHunter.Infrastructure.Persistence.Models;

namespace FreelaHunter.Infrastructure.Persistence.Mappers;

public static class OpportunityPersistenceMapper
{
    public static OpportunityRecord ToRecord(Opportunity opportunity)
    {
        return new OpportunityRecord
        {
            Id = opportunity.Id.Value,
            Marketplace = opportunity.Marketplace.ToString(),
            IntakeChannel = opportunity.IntakeChannel.ToString(),
            ExternalReference = opportunity.ExternalReference,
            Title = opportunity.Title,
            Description = opportunity.Description,
            Category = opportunity.Category,
            Subcategory = opportunity.Subcategory,
            BudgetKind = opportunity.Budget.Kind.ToString(),
            BudgetBasis = opportunity.Budget.Basis.ToString(),
            BudgetAmount = opportunity.Budget.Amount,
            BudgetMinimum = opportunity.Budget.Minimum,
            BudgetMaximum = opportunity.Budget.Maximum,
            BudgetCurrency = opportunity.Budget.Currency?.Value,
            ClientDisplayName = opportunity.Client.DisplayName,
            ClientCountry = opportunity.Client.Country,
            ClientPublishedProjects = opportunity.Client.PublishedProjects,
            ClientPaidProjects = opportunity.Client.PaidProjects,
            ClientRating = opportunity.Client.Rating,
            CompetitionProposalCount = opportunity.Competition.ProposalCount,
            CompetitionInterestedFreelancerCount = opportunity.Competition.InterestedFreelancerCount,
            PublishedAt = opportunity.PublishedAt,
            PublishedAtUtc = opportunity.PublishedAt?.UtcDateTime,
            ClientDeadline = opportunity.ClientDeadline,
            CapturedAt = opportunity.CapturedAt,
            CapturedAtUtc = opportunity.CapturedAt.UtcDateTime,
            Status = opportunity.Status.ToString(),
            Skills = opportunity.RequiredSkills.Select(s => new OpportunitySkillRecord
            {
                OpportunityId = opportunity.Id.Value,
                Skill = s
            }).ToList()
        };
    }

    public static Opportunity ToDomain(OpportunityRecord record)
    {
        var id = OpportunityId.From(record.Id);
        var marketplace = Enum.Parse<Marketplace>(record.Marketplace);
        var intakeChannel = Enum.Parse<IntakeChannel>(record.IntakeChannel);
        var status = Enum.Parse<OpportunityStatus>(record.Status);

        var currency = record.BudgetCurrency is not null
            ? CurrencyCode.From(record.BudgetCurrency)
            : null;

        var budget = record.BudgetKind switch
        {
            nameof(BudgetKind.Unknown) => Budget.Unknown(),
            nameof(BudgetKind.Negotiable) => Budget.Negotiable(currency, Enum.Parse<BudgetBasis>(record.BudgetBasis)),
            nameof(BudgetKind.Exact) => Budget.Exact(record.BudgetAmount!.Value, currency!,
                Enum.Parse<BudgetBasis>(record.BudgetBasis)),
            nameof(BudgetKind.Range) => Budget.Range(record.BudgetMinimum!.Value, record.BudgetMaximum!.Value,
                currency!, Enum.Parse<BudgetBasis>(record.BudgetBasis)),
            nameof(BudgetKind.UpTo) => Budget.UpTo(record.BudgetMaximum!.Value, currency!,
                Enum.Parse<BudgetBasis>(record.BudgetBasis)),
            nameof(BudgetKind.From) => Budget.From(record.BudgetMinimum!.Value, currency!,
                Enum.Parse<BudgetBasis>(record.BudgetBasis)),
            _ => Budget.Unknown()
        };

        var client = ClientSnapshot.Create(
            record.ClientDisplayName,
            record.ClientCountry,
            record.ClientPublishedProjects,
            record.ClientPaidProjects,
            record.ClientRating);

        var competition = CompetitionSnapshot.Create(
            record.CompetitionProposalCount,
            record.CompetitionInterestedFreelancerCount);

        var skills = record.Skills.Select(s => s.Skill).ToList().AsReadOnly();

        var publishedAt = record.PublishedAtUtc.HasValue
            ? new DateTimeOffset(record.PublishedAtUtc.Value, TimeSpan.Zero)
            : (DateTimeOffset?)null;

        var capturedAt = new DateTimeOffset(record.CapturedAtUtc, TimeSpan.Zero);

        return Opportunity.Rehydrate(
            id,
            marketplace,
            intakeChannel,
            record.ExternalReference,
            record.Title,
            record.Description,
            record.Category,
            record.Subcategory,
            budget,
            client,
            competition,
            skills,
            publishedAt,
            record.ClientDeadline,
            capturedAt,
            status);
    }

    public static void UpdateRecord(OpportunityRecord record, Opportunity opportunity)
    {
        record.Status = opportunity.Status.ToString();
        record.Skills.Clear();

        foreach (var skill in opportunity.RequiredSkills)
        {
            record.Skills.Add(new OpportunitySkillRecord
            {
                OpportunityId = record.Id,
                Skill = skill
            });
        }
    }
}

using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Infrastructure.Persistence.Mappers;
using FreelaHunter.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelaHunter.Infrastructure.Persistence.Repositories;

public sealed class EfOpportunityQueryRepository : IOpportunityQueryRepository
{
    private readonly FreelaHunterDbContext _context;

    public EfOpportunityQueryRepository(FreelaHunterDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<OpportunityInboxItem>> SearchAsync(
        OpportunityQueryCriteria criteria,
        CancellationToken cancellationToken)
    {
        var baseQuery = _context.Opportunities
            .AsNoTracking()
            .AsQueryable();

        baseQuery = ApplyFilters(baseQuery, criteria);
        baseQuery = ApplyDateFilters(baseQuery, criteria);
        baseQuery = ApplySearch(baseQuery, criteria.Search);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var sortedQuery = ApplySort(baseQuery, criteria.Sort);

        var pagedRecords = await sortedQuery
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToListAsync(cancellationToken);

        var pagedIds = pagedRecords.Select(r => r.Id).ToList();

        var skillsLookup = await _context.OpportunitySkills
            .AsNoTracking()
            .Where(s => pagedIds.Contains(s.OpportunityId))
            .GroupBy(s => s.OpportunityId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(s => s.Skill).ToList(),
                cancellationToken);

        var items = pagedRecords
            .Select(record => MapToInboxItem(record, skillsLookup))
            .ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / criteria.PageSize);

        return new PagedResult<OpportunityInboxItem>
        {
            Items = items,
            Page = criteria.Page,
            PageSize = criteria.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static IQueryable<OpportunityRecord> ApplyFilters(
        IQueryable<OpportunityRecord> query,
        OpportunityQueryCriteria criteria)
    {
        if (criteria.Marketplace.HasValue)
        {
            var marketplace = criteria.Marketplace.Value.ToString();
            query = query.Where(o => o.Marketplace == marketplace);
        }

        if (criteria.IntakeChannel.HasValue)
        {
            var intakeChannel = criteria.IntakeChannel.Value.ToString();
            query = query.Where(o => o.IntakeChannel == intakeChannel);
        }

        if (criteria.Status.HasValue)
        {
            var status = criteria.Status.Value.ToString();
            query = query.Where(o => o.Status == status);
        }

        return query;
    }

    private static IQueryable<OpportunityRecord> ApplyDateFilters(
        IQueryable<OpportunityRecord> query,
        OpportunityQueryCriteria criteria)
    {
        if (criteria.CapturedFrom.HasValue)
        {
            var fromUtc = criteria.CapturedFrom.Value.UtcDateTime;
            query = query.Where(o => o.CapturedAtUtc >= fromUtc);
        }

        if (criteria.CapturedTo.HasValue)
        {
            var toUtc = criteria.CapturedTo.Value.UtcDateTime;
            query = query.Where(o => o.CapturedAtUtc <= toUtc);
        }

        return query;
    }

    private static IQueryable<OpportunityRecord> ApplySearch(
        IQueryable<OpportunityRecord> query,
        string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim().ToLower();
        query = query.Where(o =>
            o.Title.ToLower().Contains(term) ||
            o.Description.ToLower().Contains(term) ||
            (o.ExternalReference != null && o.ExternalReference.ToLower().Contains(term)) ||
            (o.Category != null && o.Category.ToLower().Contains(term)) ||
            (o.Subcategory != null && o.Subcategory.ToLower().Contains(term)) ||
            (o.ClientDisplayName != null && o.ClientDisplayName.ToLower().Contains(term)) ||
            o.Skills.Any(s => s.Skill.ToLower().Contains(term)));

        return query;
    }

    private static IQueryable<OpportunityRecord> ApplySort(
        IQueryable<OpportunityRecord> query,
        OpportunitySort sort)
    {
        return sort switch
        {
            OpportunitySort.CapturedAtDesc => query.OrderByDescending(o => o.CapturedAtUtc),
            OpportunitySort.CapturedAtAsc => query.OrderBy(o => o.CapturedAtUtc),
            OpportunitySort.PublishedAtDesc => query
                .OrderBy(o => o.PublishedAtUtc == null)
                .ThenByDescending(o => o.PublishedAtUtc),
            OpportunitySort.PublishedAtAsc => query
                .OrderBy(o => o.PublishedAtUtc == null)
                .ThenBy(o => o.PublishedAtUtc),
            OpportunitySort.TitleAsc => query.OrderBy(o => o.Title),
            OpportunitySort.TitleDesc => query.OrderByDescending(o => o.Title),
            _ => query.OrderByDescending(o => o.CapturedAtUtc)
        };
    }

    private static OpportunityInboxItem MapToInboxItem(
        OpportunityRecord record,
        Dictionary<Guid, List<string>> skillsLookup)
    {
        var skills = skillsLookup.TryGetValue(record.Id, out var s)
            ? s.AsReadOnly()
            : Array.Empty<string>().AsReadOnly();

        return new OpportunityInboxItem
        {
            Id = record.Id,
            Marketplace = Enum.Parse<Marketplace>(record.Marketplace),
            IntakeChannel = Enum.Parse<IntakeChannel>(record.IntakeChannel),
            ExternalReference = record.ExternalReference,
            Title = record.Title,
            Category = record.Category,
            Subcategory = record.Subcategory,
            BudgetKind = record.BudgetKind,
            BudgetBasis = record.BudgetBasis,
            BudgetAmount = record.BudgetAmount,
            BudgetMinimum = record.BudgetMinimum,
            BudgetMaximum = record.BudgetMaximum,
            BudgetCurrency = record.BudgetCurrency,
            ClientDisplayName = record.ClientDisplayName,
            ClientCountry = record.ClientCountry,
            ClientPublishedProjects = record.ClientPublishedProjects,
            ClientPaidProjects = record.ClientPaidProjects,
            ClientRating = record.ClientRating,
            CompetitionProposalCount = record.CompetitionProposalCount,
            CompetitionInterestedFreelancerCount = record.CompetitionInterestedFreelancerCount,
            RequiredSkills = skills,
            PublishedAt = record.PublishedAtUtc.HasValue
                ? new DateTimeOffset(record.PublishedAtUtc.Value, TimeSpan.Zero)
                : (DateTimeOffset?)null,
            ClientDeadline = record.ClientDeadline,
            CapturedAt = new DateTimeOffset(record.CapturedAtUtc, TimeSpan.Zero),
            Status = Enum.Parse<OpportunityStatus>(record.Status)
        };
    }
}

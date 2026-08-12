using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;
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
        var query = _context.Opportunities
            .AsNoTracking()
            .Include(o => o.Skills)
            .AsQueryable();

        query = ApplyFilters(query, criteria);
        query = ApplySearch(query, criteria.Search);

        var all = await query.ToListAsync(cancellationToken);

        all = ApplyFiltersInMemory(all, criteria);

        var totalCount = all.Count;

        all = ApplySortInMemory(all, criteria.Sort);

        var items = all
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(MapToInboxItem)
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

    private static List<OpportunityRecord> ApplyFiltersInMemory(
        List<OpportunityRecord> items,
        OpportunityQueryCriteria criteria)
    {
        if (criteria.CapturedFrom.HasValue)
        {
            items = items.Where(o => o.CapturedAt >= criteria.CapturedFrom.Value).ToList();
        }

        if (criteria.CapturedTo.HasValue)
        {
            items = items.Where(o => o.CapturedAt <= criteria.CapturedTo.Value).ToList();
        }

        return items;
    }

    private static List<OpportunityRecord> ApplySortInMemory(
        List<OpportunityRecord> items,
        OpportunitySort sort)
    {
        return sort switch
        {
            OpportunitySort.CapturedAtDesc => items.OrderByDescending(o => o.CapturedAt).ToList(),
            OpportunitySort.CapturedAtAsc => items.OrderBy(o => o.CapturedAt).ToList(),
            OpportunitySort.PublishedAtDesc => items.OrderByDescending(o => o.PublishedAt ?? DateTimeOffset.MinValue).ToList(),
            OpportunitySort.PublishedAtAsc => items.OrderBy(o => o.PublishedAt ?? DateTimeOffset.MaxValue).ToList(),
            OpportunitySort.TitleAsc => items.OrderBy(o => o.Title).ToList(),
            OpportunitySort.TitleDesc => items.OrderByDescending(o => o.Title).ToList(),
            _ => items.OrderByDescending(o => o.CapturedAt).ToList()
        };
    }

    private static OpportunityInboxItem MapToInboxItem(OpportunityRecord record)
    {
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
            RequiredSkills = record.Skills.Select(s => s.Skill).ToList().AsReadOnly(),
            PublishedAt = record.PublishedAt,
            ClientDeadline = record.ClientDeadline,
            CapturedAt = record.CapturedAt,
            Status = Enum.Parse<OpportunityStatus>(record.Status)
        };
    }
}

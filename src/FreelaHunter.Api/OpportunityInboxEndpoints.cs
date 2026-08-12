using System.Net;
using FreelaHunter.Application;
using FreelaHunter.Contracts.Inbox;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FreelaHunter.Api;

public static class OpportunityInboxEndpoints
{
    public static void MapOpportunityInboxEndpoints(this WebApplication app)
    {
        app.MapGet("/api/opportunities", async (
            [FromServices] ListOpportunitiesHandler handler,
            [AsParameters] OpportunityListRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var criteria = MapCriteria(request);
                var result = await handler.HandleAsync(criteria, cancellationToken);
                return Results.Ok(MapToPagedResponse(result));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Invalid query",
                    Detail = ex.Message
                });
            }
        })
        .WithName("ListOpportunities")
        .Produces<PagedOpportunityResponse>((int)HttpStatusCode.OK)
        .ProducesProblem((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/opportunities/{id:guid}/shortlist", async (
            Guid id,
            [FromServices] ShortlistOpportunityHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                return result is not null ? Results.Ok(result) : Results.NotFound();
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Invalid transition",
                    Detail = ex.Message
                });
            }
        })
        .WithName("ShortlistOpportunity")
        .Produces<OpportunityResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/opportunities/{id:guid}/ignore", async (
            Guid id,
            [FromServices] IgnoreOpportunityHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                return result is not null ? Results.Ok(result) : Results.NotFound();
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Invalid transition",
                    Detail = ex.Message
                });
            }
        })
        .WithName("IgnoreOpportunity")
        .Produces<OpportunityResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/opportunities/{id:guid}/archive", async (
            Guid id,
            [FromServices] ArchiveOpportunityHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                return result is not null ? Results.Ok(result) : Results.NotFound();
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Invalid transition",
                    Detail = ex.Message
                });
            }
        })
        .WithName("ArchiveOpportunity")
        .Produces<OpportunityResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.BadRequest);
    }

    private static OpportunityQueryCriteria MapCriteria(OpportunityListRequest request)
    {
        return new OpportunityQueryCriteria
        {
            Page = request.Page ?? 1,
            PageSize = request.PageSize ?? 25,
            Search = request.Search,
            Marketplace = request.Marketplace.HasValue
                ? MapMarketplace(request.Marketplace.Value)
                : null,
            IntakeChannel = request.IntakeChannel.HasValue
                ? MapIntakeChannel(request.IntakeChannel.Value)
                : null,
            Status = request.Status.HasValue
                ? MapStatus(request.Status.Value)
                : null,
            CapturedFrom = request.CapturedFrom,
            CapturedTo = request.CapturedTo,
            Sort = request.Sort.HasValue
                ? MapSort(request.Sort.Value)
                : OpportunitySort.CapturedAtDesc
        };
    }

    private static Marketplace MapMarketplace(ContractMarketplace contract) => contract switch
    {
        ContractMarketplace.Workana => Marketplace.Workana,
        ContractMarketplace.Upwork => Marketplace.Upwork,
        ContractMarketplace.Freelancer => Marketplace.Freelancer,
        ContractMarketplace.Other => Marketplace.Other,
        _ => Marketplace.Unknown
    };

    private static IntakeChannel MapIntakeChannel(ContractIntakeChannel contract) => contract switch
    {
        ContractIntakeChannel.Email => IntakeChannel.Email,
        _ => IntakeChannel.Manual
    };

    private static OpportunityStatus MapStatus(ContractOpportunityStatus contract) => contract switch
    {
        ContractOpportunityStatus.Shortlisted => OpportunityStatus.Shortlisted,
        ContractOpportunityStatus.Ignored => OpportunityStatus.Ignored,
        ContractOpportunityStatus.Archived => OpportunityStatus.Archived,
        _ => OpportunityStatus.New
    };

    private static OpportunitySort MapSort(ContractOpportunitySort contract) => contract switch
    {
        ContractOpportunitySort.CapturedAtAsc => OpportunitySort.CapturedAtAsc,
        ContractOpportunitySort.PublishedAtDesc => OpportunitySort.PublishedAtDesc,
        ContractOpportunitySort.PublishedAtAsc => OpportunitySort.PublishedAtAsc,
        ContractOpportunitySort.TitleAsc => OpportunitySort.TitleAsc,
        ContractOpportunitySort.TitleDesc => OpportunitySort.TitleDesc,
        _ => OpportunitySort.CapturedAtDesc
    };

    private static PagedOpportunityResponse MapToPagedResponse(PagedResult<OpportunityInboxItem> result)
    {
        return new PagedOpportunityResponse
        {
            Items = result.Items.Select(MapToInboxItemResponse).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        };
    }

    private static OpportunityInboxItemResponse MapToInboxItemResponse(OpportunityInboxItem item)
    {
        return new OpportunityInboxItemResponse
        {
            Id = item.Id,
            Marketplace = MapToContractMarketplace(item.Marketplace),
            IntakeChannel = MapToContractIntakeChannel(item.IntakeChannel),
            ExternalReference = item.ExternalReference,
            Title = item.Title,
            Category = item.Category,
            Subcategory = item.Subcategory,
            Budget = new BudgetResponse
            {
                Kind = MapToContractBudgetKind(Enum.Parse<BudgetKind>(item.BudgetKind)),
                Basis = MapToContractBudgetBasis(Enum.Parse<BudgetBasis>(item.BudgetBasis)),
                Amount = item.BudgetAmount,
                Minimum = item.BudgetMinimum,
                Maximum = item.BudgetMaximum,
                Currency = item.BudgetCurrency
            },
            Client = new ClientResponse
            {
                DisplayName = item.ClientDisplayName,
                Country = item.ClientCountry,
                PublishedProjects = item.ClientPublishedProjects,
                PaidProjects = item.ClientPaidProjects,
                Rating = item.ClientRating
            },
            Competition = new CompetitionResponse
            {
                ProposalCount = item.CompetitionProposalCount,
                InterestedFreelancerCount = item.CompetitionInterestedFreelancerCount
            },
            RequiredSkills = [.. item.RequiredSkills],
            PublishedAt = item.PublishedAt,
            ClientDeadline = item.ClientDeadline,
            CapturedAt = item.CapturedAt,
            Status = MapToContractStatus(item.Status)
        };
    }

    private static ContractMarketplace MapToContractMarketplace(Marketplace domain) => domain switch
    {
        Marketplace.Workana => ContractMarketplace.Workana,
        Marketplace.Upwork => ContractMarketplace.Upwork,
        Marketplace.Freelancer => ContractMarketplace.Freelancer,
        Marketplace.Other => ContractMarketplace.Other,
        _ => ContractMarketplace.Unknown
    };

    private static ContractIntakeChannel MapToContractIntakeChannel(IntakeChannel domain) => domain switch
    {
        IntakeChannel.Email => ContractIntakeChannel.Email,
        _ => ContractIntakeChannel.Manual
    };

    private static ContractBudgetKind MapToContractBudgetKind(BudgetKind domain) => domain switch
    {
        BudgetKind.Negotiable => ContractBudgetKind.Negotiable,
        BudgetKind.Exact => ContractBudgetKind.Exact,
        BudgetKind.Range => ContractBudgetKind.Range,
        BudgetKind.UpTo => ContractBudgetKind.UpTo,
        BudgetKind.From => ContractBudgetKind.From,
        _ => ContractBudgetKind.Unknown
    };

    private static ContractBudgetBasis MapToContractBudgetBasis(BudgetBasis domain) => domain switch
    {
        BudgetBasis.FixedProject => ContractBudgetBasis.FixedProject,
        BudgetBasis.Hourly => ContractBudgetBasis.Hourly,
        BudgetBasis.Monthly => ContractBudgetBasis.Monthly,
        _ => ContractBudgetBasis.Unknown
    };

    private static ContractOpportunityStatus MapToContractStatus(OpportunityStatus domain) => domain switch
    {
        OpportunityStatus.Shortlisted => ContractOpportunityStatus.Shortlisted,
        OpportunityStatus.Ignored => ContractOpportunityStatus.Ignored,
        OpportunityStatus.Archived => ContractOpportunityStatus.Archived,
        _ => ContractOpportunityStatus.New
    };
}

public sealed class OpportunityListRequest
{
    [FromQuery(Name = "page")]
    public int? Page { get; set; }

    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; set; }

    [FromQuery(Name = "search")]
    public string? Search { get; set; }

    [FromQuery(Name = "marketplace")]
    public ContractMarketplace? Marketplace { get; set; }

    [FromQuery(Name = "intakeChannel")]
    public ContractIntakeChannel? IntakeChannel { get; set; }

    [FromQuery(Name = "status")]
    public ContractOpportunityStatus? Status { get; set; }

    [FromQuery(Name = "capturedFrom")]
    public DateTimeOffset? CapturedFrom { get; set; }

    [FromQuery(Name = "capturedTo")]
    public DateTimeOffset? CapturedTo { get; set; }

    [FromQuery(Name = "sort")]
    public ContractOpportunitySort? Sort { get; set; }
}

public enum ContractOpportunitySort
{
    CapturedAtDesc = 0,
    CapturedAtAsc = 1,
    PublishedAtDesc = 2,
    PublishedAtAsc = 3,
    TitleAsc = 4,
    TitleDesc = 5
}

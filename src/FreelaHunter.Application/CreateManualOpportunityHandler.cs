using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class CreateManualOpportunityHandler
{
    private readonly IOpportunityRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateManualOpportunityHandler(IOpportunityRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<OpportunityResponse> HandleAsync(
        CreateManualOpportunityRequest request,
        CancellationToken cancellationToken)
    {
        var budget = MapBudget(request.Budget);
        var client = MapClient(request.Client);
        var competition = MapCompetition(request.Competition);

        var capturedAt = _timeProvider.GetUtcNow();

        var opportunity = Domain.Entities.Opportunity.Create(
            marketplace: MapMarketplace(request.Marketplace),
            intakeChannel: IntakeChannel.Manual,
            title: request.Title,
            description: request.Description,
            budget: budget,
            client: client,
            competition: competition,
            capturedAt: capturedAt,
            externalReference: request.ExternalReference,
            category: request.Category,
            subcategory: request.Subcategory,
            requiredSkills: request.RequiredSkills,
            publishedAt: request.PublishedAt,
            clientDeadline: request.ClientDeadline);

        await _repository.AddAsync(opportunity, cancellationToken);

        return MapToResponse(opportunity);
    }

    private static Marketplace MapMarketplace(ContractMarketplace contract)
    {
        return contract switch
        {
            ContractMarketplace.Unknown => Marketplace.Unknown,
            ContractMarketplace.Workana => Marketplace.Workana,
            ContractMarketplace.Upwork => Marketplace.Upwork,
            ContractMarketplace.Freelancer => Marketplace.Freelancer,
            ContractMarketplace.Other => Marketplace.Other,
            _ => throw new DomainException($"Unknown marketplace: {contract}")
        };
    }

    private static Budget MapBudget(BudgetInput? input)
    {
        if (input is null)
            return Budget.Unknown();

        CurrencyCode? currency = null;
        if (!string.IsNullOrWhiteSpace(input.Currency))
            currency = CurrencyCode.From(input.Currency);

        return input.Kind switch
        {
            ContractBudgetKind.Unknown => Budget.Unknown(),
            ContractBudgetKind.Negotiable => Budget.Negotiable(currency),
            ContractBudgetKind.Exact => Budget.Exact(
                input.Amount ?? throw new DomainException("Budget.Exact requires Amount."),
                currency ?? throw new DomainException("Budget.Exact requires Currency.")),
            ContractBudgetKind.Range => Budget.Range(
                input.Minimum ?? throw new DomainException("Budget.Range requires Minimum."),
                input.Maximum ?? throw new DomainException("Budget.Range requires Maximum."),
                currency ?? throw new DomainException("Budget.Range requires Currency.")),
            ContractBudgetKind.UpTo => Budget.UpTo(
                input.Maximum ?? throw new DomainException("Budget.UpTo requires Maximum."),
                currency ?? throw new DomainException("Budget.UpTo requires Currency.")),
            ContractBudgetKind.From => Budget.From(
                input.Minimum ?? throw new DomainException("Budget.From requires Minimum."),
                currency ?? throw new DomainException("Budget.From requires Currency.")),
            _ => throw new DomainException($"Unknown BudgetKind: {input.Kind}")
        };
    }

    private static ClientSnapshot MapClient(ClientInput? input)
    {
        if (input is null)
            return ClientSnapshot.Create();

        return ClientSnapshot.Create(
            displayName: input.DisplayName,
            country: input.Country,
            publishedProjects: input.PublishedProjects,
            paidProjects: input.PaidProjects,
            rating: input.Rating);
    }

    private static CompetitionSnapshot MapCompetition(CompetitionInput? input)
    {
        if (input is null)
            return CompetitionSnapshot.Create();

        return CompetitionSnapshot.Create(
            proposalCount: input.ProposalCount,
            interestedFreelancerCount: input.InterestedFreelancerCount);
    }

    private static OpportunityResponse MapToResponse(Domain.Entities.Opportunity opportunity)
    {
        return new OpportunityResponse
        {
            Id = opportunity.Id.Value,
            Marketplace = MapToContractMarketplace(opportunity.Marketplace),
            IntakeChannel = MapToContractIntakeChannel(opportunity.IntakeChannel),
            ExternalReference = opportunity.ExternalReference,
            Title = opportunity.Title,
            Description = opportunity.Description,
            Category = opportunity.Category,
            Subcategory = opportunity.Subcategory,
            Budget = new BudgetResponse
            {
                Kind = MapToContractBudgetKind(opportunity.Budget.Kind),
                Amount = opportunity.Budget.Amount,
                Minimum = opportunity.Budget.Minimum,
                Maximum = opportunity.Budget.Maximum,
                Currency = opportunity.Budget.Currency?.Value
            },
            Client = new ClientResponse
            {
                DisplayName = opportunity.Client.DisplayName,
                Country = opportunity.Client.Country,
                PublishedProjects = opportunity.Client.PublishedProjects,
                PaidProjects = opportunity.Client.PaidProjects,
                Rating = opportunity.Client.Rating
            },
            Competition = new CompetitionResponse
            {
                ProposalCount = opportunity.Competition.ProposalCount,
                InterestedFreelancerCount = opportunity.Competition.InterestedFreelancerCount
            },
            RequiredSkills = [.. opportunity.RequiredSkills],
            PublishedAt = opportunity.PublishedAt,
            ClientDeadline = opportunity.ClientDeadline,
            CapturedAt = opportunity.CapturedAt,
            Status = MapToContractStatus(opportunity.Status)
        };
    }

    private static ContractMarketplace MapToContractMarketplace(Marketplace domain) => domain switch
    {
        Marketplace.Unknown => ContractMarketplace.Unknown,
        Marketplace.Workana => ContractMarketplace.Workana,
        Marketplace.Upwork => ContractMarketplace.Upwork,
        Marketplace.Freelancer => ContractMarketplace.Freelancer,
        Marketplace.Other => ContractMarketplace.Other,
        _ => ContractMarketplace.Unknown
    };

    private static ContractIntakeChannel MapToContractIntakeChannel(IntakeChannel domain) => domain switch
    {
        IntakeChannel.Manual => ContractIntakeChannel.Manual,
        IntakeChannel.Email => ContractIntakeChannel.Email,
        _ => ContractIntakeChannel.Manual
    };

    private static ContractBudgetKind MapToContractBudgetKind(BudgetKind domain) => domain switch
    {
        BudgetKind.Unknown => ContractBudgetKind.Unknown,
        BudgetKind.Negotiable => ContractBudgetKind.Negotiable,
        BudgetKind.Exact => ContractBudgetKind.Exact,
        BudgetKind.Range => ContractBudgetKind.Range,
        BudgetKind.UpTo => ContractBudgetKind.UpTo,
        BudgetKind.From => ContractBudgetKind.From,
        _ => ContractBudgetKind.Unknown
    };

    private static ContractOpportunityStatus MapToContractStatus(OpportunityStatus domain) => domain switch
    {
        OpportunityStatus.New => ContractOpportunityStatus.New,
        OpportunityStatus.Shortlisted => ContractOpportunityStatus.Shortlisted,
        OpportunityStatus.Ignored => ContractOpportunityStatus.Ignored,
        OpportunityStatus.Archived => ContractOpportunityStatus.Archived,
        _ => ContractOpportunityStatus.New
    };
}

using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Enums;
using FreelaHunter.Domain.ValueObjects;

namespace FreelaHunter.Application;

public class GetOpportunityByIdHandler
{
    private readonly IOpportunityRepository _repository;

    public GetOpportunityByIdHandler(IOpportunityRepository repository)
    {
        _repository = repository;
    }

    public async Task<OpportunityResponse?> HandleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var opportunityId = OpportunityId.From(id);
        var opportunity = await _repository.GetByIdAsync(opportunityId, cancellationToken);

        if (opportunity is null)
            return null;

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

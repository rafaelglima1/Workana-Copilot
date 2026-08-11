namespace FreelaHunter.Application;

public interface IOpportunityIntelligenceGateway
{
    Task<OpportunitySemanticAnalysis> AnalyzeAsync(
        OpportunityIntelligenceInput input,
        CancellationToken cancellationToken);
}

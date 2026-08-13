using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application;

public sealed record OpportunityProcessingResult
{
    public int Evaluated { get; init; }
    public int ProposalsGenerated { get; init; }
    public int Failed { get; init; }
}

public sealed class OpportunityBackgroundProcessor
{
    private readonly ListOpportunitiesHandler _listHandler;
    private readonly EvaluateOpportunityHandler _evaluateHandler;
    private readonly GenerateProposalStrategyHandler _proposalHandler;

    public OpportunityBackgroundProcessor(
        ListOpportunitiesHandler listHandler,
        EvaluateOpportunityHandler evaluateHandler,
        GenerateProposalStrategyHandler proposalHandler)
    {
        _listHandler = listHandler;
        _evaluateHandler = evaluateHandler;
        _proposalHandler = proposalHandler;
    }

    public async Task<OpportunityProcessingResult> ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(batchSize, 1, 100);

        var criteria = new OpportunityQueryCriteria
        {
            Page = 1,
            PageSize = pageSize,
            Evaluated = false,
            Sort = OpportunitySort.CapturedAtDesc
        };

        var page = await _listHandler.HandleAsync(criteria, cancellationToken);

        var evaluated = 0;
        var proposals = 0;
        var failed = 0;

        foreach (var item in page.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var evaluation = await _evaluateHandler.HandleAsync(item.Id, cancellationToken);
                if (evaluation is null)
                    continue;

                evaluated++;

                if (evaluation.Score.Recommendation is OpportunityRecommendation.Prioritize
                    or OpportunityRecommendation.Apply)
                {
                    var proposal = await _proposalHandler.HandleAsync(item.Id, cancellationToken);
                    if (proposal is not null)
                        proposals++;
                }
            }
            catch (Exception)
            {
                failed++;
            }
        }

        return new OpportunityProcessingResult
        {
            Evaluated = evaluated,
            ProposalsGenerated = proposals,
            Failed = failed
        };
    }
}

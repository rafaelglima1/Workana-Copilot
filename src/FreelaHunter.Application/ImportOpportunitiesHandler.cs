using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Exceptions;

namespace FreelaHunter.Application;

public class ImportOpportunitiesHandler
{
    private readonly CreateManualOpportunityHandler _createHandler;

    public ImportOpportunitiesHandler(CreateManualOpportunityHandler createHandler)
    {
        _createHandler = createHandler;
    }

    public async Task<ImportOpportunitiesResponse> HandleAsync(
        ImportOpportunitiesRequest request,
        CancellationToken cancellationToken)
    {
        var imported = 0;
        var failed = 0;
        var errors = new List<ImportErrorDetail>();

        for (var i = 0; i < request.Items.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _createHandler.HandleAsync(request.Items[i], cancellationToken);
                imported++;
            }
            catch (DomainException ex)
            {
                failed++;
                errors.Add(new ImportErrorDetail
                {
                    Index = i,
                    Title = request.Items[i].Title,
                    Error = ex.Message
                });
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add(new ImportErrorDetail
                {
                    Index = i,
                    Title = request.Items[i].Title,
                    Error = $"Unexpected error: {ex.Message}"
                });
            }
        }

        return new ImportOpportunitiesResponse
        {
            Imported = imported,
            Failed = failed,
            Errors = errors
        };
    }
}

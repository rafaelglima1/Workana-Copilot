using System.Net;
using FreelaHunter.Application;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FreelaHunter.Api;

public static class ProposalStrategyEndpoints
{
    public static void MapProposalStrategyEndpoints(this WebApplication app)
    {
        app.MapPost("/api/opportunities/{id:guid}/proposal-strategy", async (
            Guid id,
            [FromServices] GenerateProposalStrategyHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, cancellationToken);

                if (result is null)
                    return Results.NotFound();

                return Results.Ok(result);
            }
            catch (DomainException ex)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Invalid input",
                    Detail = ex.Message
                });
            }
        })
        .WithName("GenerateProposalStrategy")
        .Produces<ProposalStrategyResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.BadRequest);
    }
}

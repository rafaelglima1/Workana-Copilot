using System.Net;
using FreelaHunter.Application;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FreelaHunter.Api;

public static class OpportunityEndpoints
{
    public static void MapOpportunityEndpoints(this WebApplication app)
    {
        app.MapPost("/api/opportunities/manual", async (
            [FromBody] CreateManualOpportunityRequest request,
            [FromServices] CreateManualOpportunityHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(request, cancellationToken);
                return Results.Created($"/api/opportunities/{result.Id}", result);
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
        .WithName("CreateManualOpportunity")
        .Produces<OpportunityResponse>((int)HttpStatusCode.Created)
        .ProducesProblem((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/opportunities/{id:guid}", async (
            Guid id,
            [FromServices] GetOpportunityByIdHandler handler,
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
                    Title = "Invalid input",
                    Detail = ex.Message
                });
            }
        })
        .WithName("GetOpportunityById")
        .Produces<OpportunityResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.BadRequest);
    }
}

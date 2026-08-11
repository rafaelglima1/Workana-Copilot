using System.Net;
using FreelaHunter.Application;
using FreelaHunter.Contracts.FreelancerProfile;
using FreelaHunter.Contracts.Opportunities;
using FreelaHunter.Domain.Exceptions;
using FreelaHunter.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace FreelaHunter.Api;

public static class FreelancerProfileEndpoints
{
    public static void MapFreelancerProfileEndpoints(this WebApplication app)
    {
        app.MapPut("/api/freelancer-profile", async (
            [FromBody] SaveFreelancerProfileRequest request,
            [FromServices] SaveFreelancerProfileHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(request, cancellationToken);
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
        .WithName("SaveFreelancerProfile")
        .Produces<FreelancerProfileResponse>((int)HttpStatusCode.OK)
        .ProducesProblem((int)HttpStatusCode.BadRequest);

        app.MapGet("/api/freelancer-profile", async (
            [FromServices] GetFreelancerProfileHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetFreelancerProfile")
        .Produces<FreelancerProfileResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound);

        app.MapPost("/api/opportunities/{id:guid}/score-with-profile", async (
            Guid id,
            [FromBody] ScoreOpportunityWithProfileRequest request,
            [FromServices] ScoreOpportunityWithProfileHandler handler,
            [FromServices] IOpportunityRepository opportunityRepository,
            [FromServices] IFreelancerProfileRepository profileRepository,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var profile = await profileRepository.GetCurrentAsync(cancellationToken);
                if (profile is null)
                    return Results.Conflict(new ProblemDetails
                    {
                        Status = (int)HttpStatusCode.Conflict,
                        Title = "Profile not configured",
                        Detail = "Freelancer profile must be configured before scoring with profile."
                    });

                var result = await handler.HandleAsync(id, request, cancellationToken);
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
        .WithName("ScoreOpportunityWithProfile")
        .Produces<OpportunityScoreResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .Produces((int)HttpStatusCode.Conflict)
        .ProducesProblem((int)HttpStatusCode.BadRequest);
    }
}

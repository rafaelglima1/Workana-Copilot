using System.Net;
using FreelaHunter.Application;
using FreelaHunter.Application.Exceptions;
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

        app.MapPost("/api/opportunities/{id:guid}/score", async (
            Guid id,
            [FromBody] ScoreOpportunityRequest request,
            [FromServices] ScoreOpportunity handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
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
        .WithName("ScoreOpportunity")
        .Produces<OpportunityScoreResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .ProducesProblem((int)HttpStatusCode.BadRequest);

        app.MapPost("/api/opportunities/{id:guid}/analyze", async (
            Guid id,
            [FromServices] AnalyzeOpportunityHandler handler,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, cancellationToken);
                if (result is null)
                    return Results.NotFound();

                var response = MapToAnalysisResponse(result);
                return Results.Ok(response);
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
            catch (OpportunityIntelligenceUnavailableException)
            {
                return Results.StatusCode((int)HttpStatusCode.ServiceUnavailable);
            }
            catch (OpportunityIntelligenceInvalidResponseException)
            {
                return Results.StatusCode((int)HttpStatusCode.BadGateway);
            }
        })
        .WithName("AnalyzeOpportunity")
        .Produces<OpportunityAnalysisResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .Produces((int)HttpStatusCode.ServiceUnavailable)
        .Produces((int)HttpStatusCode.BadGateway)
        .ProducesProblem((int)HttpStatusCode.BadRequest);
    }

    private static OpportunityAnalysisResponse MapToAnalysisResponse(
        FreelaHunter.Application.OpportunitySemanticAnalysis analysis)
    {
        return new OpportunityAnalysisResponse
        {
            EstimatedHours = analysis.EstimatedHours,
            DeliveryRisk = analysis.DeliveryRisk.ToString(),
            AiLeverage = analysis.AiLeverage.ToString(),
            Confidence = analysis.Confidence.ToString(),
            TechnicalRequirements = [.. analysis.TechnicalRequirements],
            RiskFactors = [.. analysis.RiskFactors],
            AiLeverageReasons = [.. analysis.AiLeverageReasons],
            ClarifyingQuestions = [.. analysis.ClarifyingQuestions],
            Usage = analysis.Usage is not null
                ? new LlmUsageResponse
                {
                    InputTokens = analysis.Usage.InputTokens,
                    OutputTokens = analysis.Usage.OutputTokens,
                    TotalTokens = analysis.Usage.TotalTokens,
                    Provider = analysis.Usage.Provider,
                    Model = analysis.Usage.Model
                }
                : null
        };
    }
}

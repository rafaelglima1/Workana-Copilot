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

        app.MapPost("/api/opportunities/{id:guid}/evaluate", async (
            Guid id,
            [FromServices] EvaluateOpportunityHandler handler,
            [FromServices] IOpportunityRepository opportunityRepository,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(id, cancellationToken);

                if (result is null)
                {
                    var oppId = FreelaHunter.Domain.ValueObjects.OpportunityId.From(id);
                    var opportunity = await opportunityRepository.GetByIdAsync(oppId, cancellationToken);
                    return opportunity is not null
                        ? Results.Conflict(new ProblemDetails
                        {
                            Status = (int)HttpStatusCode.Conflict,
                            Title = "Profile not configured",
                            Detail = "FreelancerProfile must be configured before evaluation."
                        })
                        : Results.NotFound();
                }

                var response = MapToEvaluationResponse(result);
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
        .WithName("EvaluateOpportunity")
        .Produces<OpportunityEvaluationResponse>((int)HttpStatusCode.OK)
        .Produces((int)HttpStatusCode.NotFound)
        .Produces((int)HttpStatusCode.Conflict)
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

    private static OpportunityEvaluationResponse MapToEvaluationResponse(
        FreelaHunter.Application.OpportunityEvaluation evaluation)
    {
        return new OpportunityEvaluationResponse
        {
            OpportunityId = evaluation.OpportunityId.Value.ToString(),
            Analysis = MapToAnalysisResponse(evaluation.Analysis),
            ScoringInputs = MapToScoringInputs(evaluation.ScoringInputs),
            Score = MapToScoreResponse(evaluation.Score)
        };
    }

    private static ScoringInputSummaryResponse MapToScoringInputs(
        FreelaHunter.Application.ScoringInputSummary inputs)
    {
        return new ScoringInputSummaryResponse
        {
            ProfileSkills = inputs.ProfileSkills
                .Select(s => new SkillCapabilitySummaryResponse
                {
                    Name = s.Name,
                    Level = s.Level
                })
                .ToList(),
            TargetHourlyRate = new HourlyRateSummaryResponse
            {
                Amount = inputs.TargetHourlyRate.Amount,
                Currency = inputs.TargetHourlyRate.Currency
            },
            EstimatedHours = inputs.EstimatedHours,
            DeliveryRisk = inputs.DeliveryRisk.ToString(),
            AiLeverage = inputs.AiLeverage.ToString(),
            TechnicalFitSource = inputs.TechnicalFitSource,
            EstimatedHoursSource = inputs.EstimatedHoursSource,
            DeliveryRiskSource = inputs.DeliveryRiskSource,
            AiLeverageSource = inputs.AiLeverageSource,
            TargetHourlyRateSource = inputs.TargetHourlyRateSource
        };
    }

    private static OpportunityScoreResponse MapToScoreResponse(
        FreelaHunter.Domain.ValueObjects.OpportunityScore score)
    {
        return new OpportunityScoreResponse
        {
            Overall = score.Overall.Value,
            Recommendation = score.Recommendation.ToString(),
            TechnicalFit = MapComponent(score.TechnicalFit),
            EconomicPotential = MapComponent(score.EconomicPotential),
            ClientQuality = MapComponent(score.ClientQuality),
            Competition = MapComponent(score.Competition),
            DeliveryConfidence = MapComponent(score.DeliveryConfidence),
            AiLeverage = MapComponent(score.AiLeverage)
        };
    }

    private static ScoreComponentResponse MapComponent(
        FreelaHunter.Domain.ValueObjects.ScoreComponent component)
    {
        return new ScoreComponentResponse
        {
            Score = component.Score.Value,
            Weight = component.Weight,
            WeightedContribution = component.WeightedContribution,
            Rules = component.Rules.Select(r => r.ToString()).ToList()
        };
    }
}

using FreelaHunter.Application;
using FreelaHunter.Domain.Enums;

namespace FreelaHunter.Application.Tests;

internal sealed class FakeIntelligenceGateway : IOpportunityIntelligenceGateway
{
    private readonly Func<OpportunityIntelligenceInput, CancellationToken, Task<OpportunitySemanticAnalysis>> _handler;
    public List<OpportunityIntelligenceInput> Inputs { get; } = [];

    public FakeIntelligenceGateway()
    {
        _handler = (input, ct) =>
        {
            Inputs.Add(input);
            return Task.FromResult(CreateDefaultAnalysis());
        };
    }

    public FakeIntelligenceGateway(
        Func<OpportunityIntelligenceInput, CancellationToken, Task<OpportunitySemanticAnalysis>> handler)
    {
        _handler = handler;
    }

    public Task<OpportunitySemanticAnalysis> AnalyzeAsync(
        OpportunityIntelligenceInput input,
        CancellationToken cancellationToken)
    {
        return _handler(input, cancellationToken);
    }

    private static OpportunitySemanticAnalysis CreateDefaultAnalysis()
    {
        return new OpportunitySemanticAnalysis
        {
            EstimatedHours = 120,
            DeliveryRisk = DeliveryRiskLevel.Moderate,
            AiLeverage = AiLeverageLevel.High,
            Confidence = AnalysisConfidence.Medium,
            TechnicalRequirements = ["C#", "ASP.NET Core"],
            RiskFactors = ["Tight deadline"],
            AiLeverageReasons = ["CRUD operations"],
            ClarifyingQuestions = ["What is the deployment target?"],
            Usage = new LlmUsage
            {
                InputTokens = 100,
                OutputTokens = 200,
                TotalTokens = 300,
                Provider = "MiMo",
                Model = "mimo-v2.5"
            }
        };
    }
}

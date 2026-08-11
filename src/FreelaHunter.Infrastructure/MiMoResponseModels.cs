using System.Text.Json.Serialization;

namespace FreelaHunter.Infrastructure;

internal sealed class MiMoChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<MiMoChoice> Choices { get; set; } = [];

    [JsonPropertyName("usage")]
    public MiMoUsage? Usage { get; set; }
}

internal sealed class MiMoChoice
{
    [JsonPropertyName("message")]
    public MiMoMessage? Message { get; set; }
}

internal sealed class MiMoMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

internal sealed class MiMoUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int? PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int? CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int? TotalTokens { get; set; }
}

internal sealed class MiMoAnalysisResult
{
    [JsonPropertyName("estimatedHours")]
    public decimal EstimatedHours { get; set; }

    [JsonPropertyName("deliveryRisk")]
    public string DeliveryRisk { get; set; } = string.Empty;

    [JsonPropertyName("aiLeverage")]
    public string AiLeverage { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = string.Empty;

    [JsonPropertyName("technicalRequirements")]
    public List<string> TechnicalRequirements { get; set; } = [];

    [JsonPropertyName("riskFactors")]
    public List<string> RiskFactors { get; set; } = [];

    [JsonPropertyName("aiLeverageReasons")]
    public List<string> AiLeverageReasons { get; set; } = [];

    [JsonPropertyName("clarifyingQuestions")]
    public List<string> ClarifyingQuestions { get; set; } = [];
}

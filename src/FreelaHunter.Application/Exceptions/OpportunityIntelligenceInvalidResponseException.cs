namespace FreelaHunter.Application.Exceptions;

public sealed class OpportunityIntelligenceInvalidResponseException : Exception
{
    public OpportunityIntelligenceInvalidResponseException(string message)
        : base(message) { }

    public OpportunityIntelligenceInvalidResponseException(string message, Exception innerException)
        : base(message, innerException) { }
}

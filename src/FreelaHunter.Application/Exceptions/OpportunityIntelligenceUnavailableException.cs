namespace FreelaHunter.Application.Exceptions;

public sealed class OpportunityIntelligenceUnavailableException : Exception
{
    public OpportunityIntelligenceUnavailableException(string message)
        : base(message) { }

    public OpportunityIntelligenceUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}

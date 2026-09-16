namespace MovieCatalog.Domain.Exceptions;

/// <summary>Thrown when a domain/business rule is violated (e.g. duplicate title). Maps to HTTP 400.</summary>
public class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(string message) : base(message)
    {
    }
}

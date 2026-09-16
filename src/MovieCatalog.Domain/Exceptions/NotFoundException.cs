namespace MovieCatalog.Domain.Exceptions;

/// <summary>Thrown when a requested aggregate does not exist (or is soft-deleted). Maps to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException ForEntity(string entityName, string id)
        => new($"{entityName} com Id '{id}' não foi encontrado.");
}

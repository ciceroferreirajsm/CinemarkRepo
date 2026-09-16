using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MovieCatalog.Domain.Enums;

namespace MovieCatalog.Domain.Entities;

/// <summary>
/// Film aggregate. Kept as a single persistence-friendly POCO (BSON attributes only,
/// no driver/connection types) instead of a separate read/write model pair -- a
/// pragmatic trade-off for this catalog's size, documented in the README.
/// </summary>
public class Film : IEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Title { get; set; } = string.Empty;

    public string? Synopsis { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Genre Genre { get; set; }

    public DateTime ReleaseDate { get; set; }

    public int DurationMinutes { get; set; }

    public decimal Rating { get; set; }

    public bool Active { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

using MovieCatalog.Domain.Entities;
using MovieCatalog.Domain.Enums;

namespace MovieCatalog.Application.Films.Dtos;

/// <summary>
/// Example:
/// {
///   "id": "662f1b7e2c1a4e0012a3f9d1",
///   "title": "Duna: Parte Dois",
///   "synopsis": "Paul Atreides se une aos Fremen para vingar sua família.",
///   "genre": "FiccaoCientifica",
///   "releaseDate": "2024-03-01T00:00:00Z",
///   "durationMinutes": 166,
///   "rating": 8.7,
///   "active": true,
///   "createdAt": "2024-03-01T12:00:00Z",
///   "updatedAt": "2024-03-01T12:00:00Z"
/// }
/// </summary>
public class FilmResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Synopsis { get; set; }
    public Genre Genre { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Rating { get; set; }
    public bool Active { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static FilmResponse FromEntity(Film film) => new()
    {
        Id = film.Id,
        Title = film.Title,
        Synopsis = film.Synopsis,
        Genre = film.Genre,
        ReleaseDate = film.ReleaseDate,
        DurationMinutes = film.DurationMinutes,
        Rating = film.Rating,
        Active = film.Active,
        CreatedAt = film.CreatedAt,
        UpdatedAt = film.UpdatedAt
    };
}

using MovieCatalog.Domain.Enums;

namespace MovieCatalog.Application.Films.Dtos;

/// <summary>
/// Example:
/// {
///   "title": "Duna: Parte Dois",
///   "synopsis": "Paul Atreides se une aos Fremen para vingar sua família.",
///   "genre": "FiccaoCientifica",
///   "releaseDate": "2024-03-01T00:00:00Z",
///   "durationMinutes": 166,
///   "rating": 8.7
/// }
/// </summary>
public class CreateFilmRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Synopsis { get; set; }
    public Genre Genre { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Rating { get; set; }
}

using MovieCatalog.Domain.Enums;

namespace MovieCatalog.Application.Films.Dtos;

/// <summary>
/// Example:
/// {
///   "title": "Duna: Parte Dois",
///   "synopsis": "Versão revisada da sinopse.",
///   "genre": "FiccaoCientifica",
///   "releaseDate": "2024-03-01T00:00:00Z",
///   "durationMinutes": 166,
///   "rating": 9.0,
///   "active": true
/// }
/// </summary>
public class UpdateFilmRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Synopsis { get; set; }
    public Genre Genre { get; set; }
    public DateTime ReleaseDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Rating { get; set; }
    public bool Active { get; set; }
}

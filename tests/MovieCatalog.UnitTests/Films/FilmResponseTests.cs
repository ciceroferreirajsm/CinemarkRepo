using FluentAssertions;
using MovieCatalog.Application.Films.Dtos;
using MovieCatalog.Domain.Entities;
using MovieCatalog.Domain.Enums;
using Xunit;

namespace MovieCatalog.UnitTests.Films;

public class FilmResponseTests
{
    [Fact]
    public void FromEntity_MapsAllFields()
    {
        var film = new Film
        {
            Id = "1",
            Title = "Duna",
            Synopsis = "Sinopse",
            Genre = Genre.FiccaoCientifica,
            ReleaseDate = new DateTime(2024, 3, 1),
            DurationMinutes = 166,
            Rating = 8.7m,
            Active = true,
            CreatedAt = new DateTime(2024, 1, 1),
            UpdatedAt = new DateTime(2024, 1, 2)
        };

        var response = FilmResponse.FromEntity(film);

        response.Id.Should().Be(film.Id);
        response.Title.Should().Be(film.Title);
        response.Synopsis.Should().Be(film.Synopsis);
        response.Genre.Should().Be(film.Genre);
        response.ReleaseDate.Should().Be(film.ReleaseDate);
        response.DurationMinutes.Should().Be(film.DurationMinutes);
        response.Rating.Should().Be(film.Rating);
        response.Active.Should().Be(film.Active);
        response.CreatedAt.Should().Be(film.CreatedAt);
        response.UpdatedAt.Should().Be(film.UpdatedAt);
    }
}

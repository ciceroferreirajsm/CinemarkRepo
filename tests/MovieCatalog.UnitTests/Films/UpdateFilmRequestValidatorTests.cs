using FluentValidation.TestHelper;
using MovieCatalog.Application.Films.Dtos;
using MovieCatalog.Application.Films.Validators;
using MovieCatalog.Domain.Enums;
using Xunit;

namespace MovieCatalog.UnitTests.Films;

public class UpdateFilmRequestValidatorTests
{
    private readonly UpdateFilmRequestValidator _validator = new();

    private static UpdateFilmRequest ValidRequest() => new()
    {
        Title = "Duna: Parte Dois",
        Synopsis = "Sinopse válida.",
        Genre = Genre.FiccaoCientifica,
        ReleaseDate = new DateTime(2024, 3, 1),
        DurationMinutes = 166,
        Rating = 9.0m,
        Active = true
    };

    [Fact]
    public void Validate_WhenRequestIsValid_HasNoErrors()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenTitleIsEmpty_HasErrorWithSpecificMessage()
    {
        var request = ValidRequest();
        request.Title = string.Empty;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title).WithErrorMessage("Título é obrigatório.");
    }

    [Fact]
    public void Validate_WhenDurationIsZeroOrNegative_HasError()
    {
        var request = ValidRequest();
        request.DurationMinutes = -5;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes);
    }

    [Fact]
    public void Validate_WhenRatingIsOutOfRange_HasError()
    {
        var request = ValidRequest();
        request.Rating = 11m;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Rating);
    }

    [Fact]
    public void Validate_WhenGenreIsInvalid_HasError()
    {
        var request = ValidRequest();
        request.Genre = (Genre)999;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Genre);
    }
}

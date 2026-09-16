using FluentValidation.TestHelper;
using MovieCatalog.Application.Films.Dtos;
using MovieCatalog.Application.Films.Validators;
using MovieCatalog.Domain.Enums;
using Xunit;

namespace MovieCatalog.UnitTests.Films;

public class CreateFilmRequestValidatorTests
{
    private readonly CreateFilmRequestValidator _validator = new();

    private static CreateFilmRequest ValidRequest() => new()
    {
        Title = "Duna: Parte Dois",
        Synopsis = "Sinopse válida.",
        Genre = Genre.FiccaoCientifica,
        ReleaseDate = new DateTime(2024, 3, 1),
        DurationMinutes = 166,
        Rating = 8.7m
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
    public void Validate_WhenTitleExceedsMaxLength_HasError()
    {
        var request = ValidRequest();
        request.Title = new string('A', 201);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Validate_WhenDurationIsZeroOrNegative_HasErrorWithSpecificMessage()
    {
        var request = ValidRequest();
        request.DurationMinutes = 0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DurationMinutes).WithErrorMessage("Duração deve ser maior que zero.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10.1)]
    public void Validate_WhenRatingIsOutOfRange_HasError(double rating)
    {
        var request = ValidRequest();
        request.Rating = (decimal)rating;

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

    [Fact]
    public void Validate_WhenReleaseDateIsDefault_HasError()
    {
        var request = ValidRequest();
        request.ReleaseDate = default;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ReleaseDate);
    }

    [Fact]
    public void Validate_WhenSynopsisExceedsMaxLength_HasError()
    {
        var request = ValidRequest();
        request.Synopsis = new string('A', 2001);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Synopsis);
    }
}

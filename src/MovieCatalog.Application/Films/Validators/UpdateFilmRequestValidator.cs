using FluentValidation;
using MovieCatalog.Application.Films.Dtos;

namespace MovieCatalog.Application.Films.Validators;

public class UpdateFilmRequestValidator : AbstractValidator<UpdateFilmRequest>
{
    public UpdateFilmRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Título é obrigatório.")
            .MaximumLength(200).WithMessage("Título deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Synopsis)
            .MaximumLength(2000).WithMessage("Sinopse deve ter no máximo 2000 caracteres.");

        RuleFor(x => x.Genre)
            .IsInEnum().WithMessage("Gênero informado é inválido.");

        RuleFor(x => x.ReleaseDate)
            .NotEqual(default(DateTime)).WithMessage("Data de lançamento é obrigatória.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("Duração deve ser maior que zero.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(0, 10).WithMessage("Avaliação deve estar entre 0 e 10.");
    }
}

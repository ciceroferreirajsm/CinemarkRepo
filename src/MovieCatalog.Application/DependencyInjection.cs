using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieCatalog.Application.Common;
using MovieCatalog.Application.Films;

namespace MovieCatalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMovieCatalogApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.AddScoped<IFilmService, FilmService>();
        services.AddValidatorsFromAssemblyContaining<FilmService>();
        return services;
    }
}

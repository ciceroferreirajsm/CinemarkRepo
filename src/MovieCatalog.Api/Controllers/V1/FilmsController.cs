using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MovieCatalog.Api.Middleware;
using MovieCatalog.Application.Common;
using MovieCatalog.Application.Films;
using MovieCatalog.Application.Films.Dtos;
using MovieCatalog.Domain.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieCatalog.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/films")]
[Produces("application/json")]
public class FilmsController : ControllerBase
{
    private readonly IFilmService _filmService;
    private readonly IValidator<CreateFilmRequest> _createValidator;
    private readonly IValidator<UpdateFilmRequest> _updateValidator;

    public FilmsController(
        IFilmService filmService,
        IValidator<CreateFilmRequest> createValidator,
        IValidator<UpdateFilmRequest> updateValidator)
    {
        _filmService = filmService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Cria um novo filme no catálogo.</summary>
    /// <remarks>
    /// Valida o corpo da requisição e a duplicidade de título (case-insensitive)
    /// antes de persistir. Publica o evento <c>FilmCreated</c> na fila SQS.
    /// </remarks>
    [HttpPost]
    [SwaggerOperation(Summary = "Cria um filme", Description = "Valida os dados, garante título único e publica o evento FilmCreated.")]
    [ProducesResponseType(typeof(FilmResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FilmResponse>> Create([FromBody] CreateFilmRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var correlationId = HttpContext.GetCorrelationId();
        var response = await _filmService.CreateAsync(request, correlationId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id, version = "1.0" }, response);
    }

    /// <summary>Obtém um filme pelo id (cache-aside, TTL configurável).</summary>
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "Obtém um filme por id", Description = "Consulta primeiro o cache Redis (films:{id}); em caso de miss, busca no MongoDB e popula o cache.")]
    [ProducesResponseType(typeof(FilmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FilmResponse>> GetById([FromRoute] string id, CancellationToken cancellationToken)
    {
        var response = await _filmService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    /// <summary>Lista filmes com filtros por gênero/status e paginação (cache-aside).</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Lista filmes", Description = "Suporta os filtros genre e active, além de page/pageSize. Resultado é cacheado por combinação de filtros.")]
    [ProducesResponseType(typeof(PagedResult<FilmResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FilmResponse>>> GetList(
        [FromQuery] Genre? genre,
        [FromQuery] bool? active,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new FilmListQuery { Genre = genre, Active = active, Page = page, PageSize = pageSize };
        var response = await _filmService.GetListAsync(query, cancellationToken);
        return Ok(response);
    }

    /// <summary>Atualiza um filme existente.</summary>
    [HttpPut("{id}")]
    [SwaggerOperation(Summary = "Atualiza um filme", Description = "Revalida a duplicidade de título quando alterado e publica o evento FilmUpdated.")]
    [ProducesResponseType(typeof(FilmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FilmResponse>> Update([FromRoute] string id, [FromBody] UpdateFilmRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var correlationId = HttpContext.GetCorrelationId();
        var response = await _filmService.UpdateAsync(id, request, correlationId, cancellationToken);
        return Ok(response);
    }

    /// <summary>Remove logicamente um filme (soft delete).</summary>
    [HttpDelete("{id}")]
    [SwaggerOperation(Summary = "Remove um filme (soft delete)", Description = "Marca IsDeleted=true e Active=false, invalida o cache relacionado e publica o evento FilmDeleted.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] string id, CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.GetCorrelationId();
        await _filmService.DeleteAsync(id, correlationId, cancellationToken);
        return NoContent();
    }
}

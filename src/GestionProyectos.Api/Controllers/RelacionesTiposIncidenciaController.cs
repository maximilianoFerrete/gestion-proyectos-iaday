using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.RelacionesTiposIncidencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de relaciones permitidas; sin GET/PUT/DELETE por id (RN-09, DA-02).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
[ApiController]
[Route("api/relaciones-tipos-incidencia")]
[Produces("application/json")]
public sealed class RelacionesTiposIncidenciaController(IRelacionTipoIncidenciaService servicio) : ControllerBase
{
    /// <summary>Lista relaciones, filtrando opcionalmente por tipo padre y/o tipo hijo.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<RelacionTipoIncidenciaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RelacionTipoIncidenciaDto>>> Listar([FromQuery] int? tipoPadreId,
        [FromQuery] int? tipoHijoId, CancellationToken ct) =>
        Ok(await servicio.ListarAsync(tipoPadreId, tipoHijoId, ct));

    /// <summary>Crea una relación permitida (el Location es informativo).</summary>
    [HttpPost]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<RelacionTipoIncidenciaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RelacionTipoIncidenciaDto>> Crear(
        [FromBody] CrearRelacionTipoIncidenciaRequest request, CancellationToken ct)
    {
        var relacion = await servicio.CrearAsync(request, ct);
        return Created($"/api/relaciones-tipos-incidencia/{relacion.Id}", relacion);
    }
}

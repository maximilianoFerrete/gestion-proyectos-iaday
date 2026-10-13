using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de incidencias, siempre bajo su proyecto (RN-35, RN-37). No hay DELETE (RN-34).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Árbol filtrable por ámbito (RF-005, C-RF002-04)
/// </remarks>
[ApiController]
[Route("api/proyectos/{proyectoId:int}/incidencias")]
[Produces("application/json")]
public sealed class IncidenciasController(IIncidenciaService servicio, IIncidenciaConsultaService consultas)
    : ControllerBase
{
    /// <summary>Lista incidencias del proyecto con filtros, orden y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<Paginado<IncidenciaResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Paginado<IncidenciaResumenDto>>> Listar(int proyectoId,
        [FromQuery] ListarIncidenciasQuery query, CancellationToken ct) =>
        Ok(await consultas.ListarAsync(proyectoId, query, ct));

    /// <summary>Incidencias del proyecto o de su ámbito en lista plana (árbol por padreId).</summary>
    [HttpGet("arbol")]
    [ProducesResponseType<IReadOnlyList<NodoArbolIncidenciaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<NodoArbolIncidenciaDto>>> Arbol(int proyectoId,
        [FromQuery] ArbolIncidenciasQuery query, CancellationToken ct) =>
        Ok(await consultas.ObtenerArbolAsync(proyectoId, query, ct));

    /// <summary>Obtiene el detalle de una incidencia del proyecto.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<IncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidenciaDto>> Obtener(int proyectoId, int id, CancellationToken ct) =>
        Ok(await consultas.ObtenerAsync(proyectoId, id, ct));

    /// <summary>Crea una incidencia en el proyecto.</summary>
    [HttpPost]
    [ProducesResponseType<IncidenciaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidenciaDto>> Crear(int proyectoId, [FromBody] CrearIncidenciaRequest request,
        CancellationToken ct)
    {
        var incidencia = await servicio.CrearAsync(proyectoId, request, ct);
        return Created($"/api/proyectos/{proyectoId}/incidencias/{incidencia.Id}", incidencia);
    }

    /// <summary>Edita una incidencia (reemplazo completo; el tipo no cambia).</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<IncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidenciaDto>> Editar(int proyectoId, int id,
        [FromBody] EditarIncidenciaRequest request, CancellationToken ct) =>
        Ok(await servicio.EditarAsync(proyectoId, id, request, ct));

    /// <summary>Cambia el tipo de una incidencia (permiso Incidencias.CambiarTipo).</summary>
    [HttpPost("{id:int}/cambiar-tipo")]
    [Authorize(Policy = Permisos.IncidenciasCambiarTipo)]
    [ProducesResponseType<IncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IncidenciaDto>> CambiarTipo(int proyectoId, int id,
        [FromBody] CambiarTipoIncidenciaRequest request, CancellationToken ct) =>
        Ok(await servicio.CambiarTipoAsync(proyectoId, id, request, ct));
}

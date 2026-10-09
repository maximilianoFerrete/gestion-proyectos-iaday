using GestionProyectos.Api.Seguridad;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de proyectos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
[ApiController]
[Route("api/proyectos")]
[Produces("application/json")]
public sealed class ProyectosController(IProyectoService servicio) : ControllerBase
{
    /// <summary>Lista proyectos con filtros, orden y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<Paginado<ProyectoResumenDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Paginado<ProyectoResumenDto>>> Listar([FromQuery] ListarProyectosQuery query,
        CancellationToken ct) =>
        Ok(await servicio.ListarAsync(query, ct));

    /// <summary>Obtiene el detalle de un proyecto.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProyectoDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un proyecto en el estado inicial con código generado.</summary>
    [HttpPost]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProyectoDto>> Crear([FromBody] CrearProyectoRequest request,
        CancellationToken ct)
    {
        var proyecto = await servicio.CrearAsync(request, ct);
        return Created($"/api/proyectos/{proyecto.Id}", proyecto);
    }

    /// <summary>Edita un proyecto no archivado.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProyectoDto>> Editar(int id, [FromBody] EditarProyectoRequest request,
        CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));

    /// <summary>Archiva el proyecto (solo administradores, irreversible).</summary>
    [HttpPost("{id:int}/archivar")]
    [Authorize(Policy = EsquemaProvisional.PoliticaSoloAdministradores)]
    [ProducesResponseType<ProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProyectoDto>> Archivar(int id, CancellationToken ct) =>
        Ok(await servicio.ArchivarAsync(id, ct));
}

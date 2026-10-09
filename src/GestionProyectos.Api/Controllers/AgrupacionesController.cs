using GestionProyectos.Application.Agrupaciones;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de agrupaciones, siempre bajo su proyecto (RN-29).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
[ApiController]
[Route("api/proyectos/{proyectoId:int}/agrupaciones")]
[Produces("application/json")]
public sealed class AgrupacionesController(IAgrupacionService servicio) : ControllerBase
{
    /// <summary>Lista las agrupaciones del proyecto ordenadas por nombre.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AgrupacionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AgrupacionDto>>> Listar(int proyectoId, CancellationToken ct) =>
        Ok(await servicio.ListarAsync(proyectoId, ct));

    /// <summary>Obtiene una agrupación del proyecto.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<AgrupacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgrupacionDto>> Obtener(int proyectoId, int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(proyectoId, id, ct));

    /// <summary>Crea una agrupación en el proyecto.</summary>
    [HttpPost]
    [ProducesResponseType<AgrupacionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AgrupacionDto>> Crear(int proyectoId, [FromBody] AgrupacionRequest request,
        CancellationToken ct)
    {
        var agrupacion = await servicio.CrearAsync(proyectoId, request, ct);
        return Created($"/api/proyectos/{proyectoId}/agrupaciones/{agrupacion.Id}", agrupacion);
    }

    /// <summary>Edita una agrupación del proyecto.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<AgrupacionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AgrupacionDto>> Editar(int proyectoId, int id,
        [FromBody] AgrupacionRequest request, CancellationToken ct) =>
        Ok(await servicio.EditarAsync(proyectoId, id, request, ct));

    /// <summary>Elimina una agrupación del proyecto.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eliminar(int proyectoId, int id, CancellationToken ct)
    {
        await servicio.EliminarAsync(proyectoId, id, ct);
        return NoContent();
    }
}

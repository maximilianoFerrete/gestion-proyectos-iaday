using GestionProyectos.Application.Seguimiento;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Tablero general de seguimiento de todos los proyectos; no exige usuario (RF-005 RN-25).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
[ApiController]
[Route("api/seguimiento")]
[Produces("application/json")]
public sealed class SeguimientoController(ISeguimientoService servicio) : ControllerBase
{
    /// <summary>Indicadores totales y una fila por proyecto, con alcance, orden y paginación.</summary>
    [HttpGet("proyectos")]
    [ProducesResponseType<TableroProyectosDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TableroProyectosDto>> TableroProyectos(
        [FromQuery] ConsultaTableroProyectosQuery query, CancellationToken ct) =>
        Ok(await servicio.ObtenerTableroProyectosAsync(query, ct));
}

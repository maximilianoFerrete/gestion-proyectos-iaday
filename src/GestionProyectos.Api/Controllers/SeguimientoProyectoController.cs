using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Seguimiento;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Seguimiento de un proyecto: resumen, grilla del tablero y configuración de columnas.
/// Los GET no exigen usuario (RN-25); el PUT exige Proyectos.ConfigurarTablero (RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
[ApiController]
[Route("api/proyectos/{proyectoId:int}/seguimiento")]
[Produces("application/json")]
public sealed class SeguimientoProyectoController(
    ISeguimientoService servicio,
    IConfiguracionTableroService configuracion) : ControllerBase
{
    /// <summary>Resumen del proyecto, de una agrupación o de las incidencias sin agrupación.</summary>
    [HttpGet]
    [ProducesResponseType<SeguimientoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SeguimientoDto>> Resumen(int proyectoId,
        [FromQuery] ConsultaSeguimientoQuery query, CancellationToken ct) =>
        Ok(await servicio.ObtenerSeguimientoAsync(proyectoId, query, ct));

    /// <summary>Grilla de incidencias del ámbito y alcance, filtrable por indicador, estado y texto.</summary>
    [HttpGet("tablero")]
    [ProducesResponseType<TableroIncidenciasDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableroIncidenciasDto>> Tablero(int proyectoId,
        [FromQuery] ConsultaTableroIncidenciasQuery query, CancellationToken ct) =>
        Ok(await servicio.ObtenerTableroAsync(proyectoId, query, ct));

    /// <summary>Columnas visibles del tablero del proyecto (por defecto, todas).</summary>
    [HttpGet("tablero/configuracion")]
    [ProducesResponseType<ConfiguracionTableroDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConfiguracionTableroDto>> ObtenerConfiguracion(int proyectoId,
        CancellationToken ct) =>
        Ok(await configuracion.ObtenerAsync(proyectoId, ct));

    /// <summary>Crea o reemplaza las columnas visibles del tablero del proyecto.</summary>
    [HttpPut("tablero/configuracion")]
    [Authorize(Policy = Permisos.ProyectosConfigurarTablero)]
    [ProducesResponseType<ConfiguracionTableroDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConfiguracionTableroDto>> GuardarConfiguracion(int proyectoId,
        [FromBody] ConfiguracionTableroRequest request, CancellationToken ct) =>
        Ok(await configuracion.GuardarAsync(proyectoId, request, ct));
}

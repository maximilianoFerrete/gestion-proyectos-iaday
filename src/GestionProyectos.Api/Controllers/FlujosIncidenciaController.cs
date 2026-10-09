using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.FlujosIncidencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de flujos de trabajo; las escrituras exigen Configuracion.Incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
[ApiController]
[Route("api/flujos-incidencia")]
[Produces("application/json")]
public sealed class FlujosIncidenciaController(IFlujoIncidenciaService servicio) : ControllerBase
{
    /// <summary>Lista los flujos ordenados por nombre.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FlujoIncidenciaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FlujoIncidenciaDto>>> Listar(CancellationToken ct) =>
        Ok(await servicio.ListarAsync(ct));

    /// <summary>Obtiene un flujo por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<FlujoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FlujoIncidenciaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un flujo con su composición de estados.</summary>
    [HttpPost]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<FlujoIncidenciaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FlujoIncidenciaDto>> Crear([FromBody] FlujoIncidenciaRequest request,
        CancellationToken ct)
    {
        var flujo = await servicio.CrearAsync(request, ct);
        return Created($"/api/flujos-incidencia/{flujo.Id}", flujo);
    }

    /// <summary>Reemplaza nombre, descripción y composición completa del flujo.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<FlujoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FlujoIncidenciaDto>> Editar(int id, [FromBody] FlujoIncidenciaRequest request,
        CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));
}

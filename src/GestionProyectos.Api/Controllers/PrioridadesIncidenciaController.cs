using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.PrioridadesIncidencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de prioridades de incidencia; las escrituras exigen Configuracion.Incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
[ApiController]
[Route("api/prioridades-incidencia")]
[Produces("application/json")]
public sealed class PrioridadesIncidenciaController(IPrioridadIncidenciaService servicio) : ControllerBase
{
    /// <summary>Lista prioridades por orden y nombre.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PrioridadIncidenciaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PrioridadIncidenciaDto>>> Listar([FromQuery] bool soloActivas,
        CancellationToken ct) =>
        Ok(await servicio.ListarAsync(soloActivas, ct));

    /// <summary>Obtiene una prioridad por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PrioridadIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrioridadIncidenciaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea una prioridad activa y no predeterminada.</summary>
    [HttpPost]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<PrioridadIncidenciaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrioridadIncidenciaDto>> Crear([FromBody] PrioridadIncidenciaRequest request,
        CancellationToken ct)
    {
        var prioridad = await servicio.CrearAsync(request, ct);
        return Created($"/api/prioridades-incidencia/{prioridad.Id}", prioridad);
    }

    /// <summary>Edita nombre, descripción y orden de una prioridad.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<PrioridadIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrioridadIncidenciaDto>> Editar(int id,
        [FromBody] PrioridadIncidenciaRequest request, CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));

    /// <summary>Marca la prioridad como predeterminada y desmarca la anterior.</summary>
    [HttpPost("{id:int}/marcar-predeterminada")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<PrioridadIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrioridadIncidenciaDto>> MarcarPredeterminada(int id, CancellationToken ct) =>
        Ok(await servicio.MarcarPredeterminadaAsync(id, ct));

    /// <summary>Desactiva la prioridad (idempotente); 409 si es la predeterminada.</summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<PrioridadIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrioridadIncidenciaDto>> Desactivar(int id, CancellationToken ct) =>
        Ok(await servicio.DesactivarAsync(id, ct));

    /// <summary>Activa la prioridad (idempotente).</summary>
    [HttpPost("{id:int}/activar")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<PrioridadIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrioridadIncidenciaDto>> Activar(int id, CancellationToken ct) =>
        Ok(await servicio.ActivarAsync(id, ct));
}

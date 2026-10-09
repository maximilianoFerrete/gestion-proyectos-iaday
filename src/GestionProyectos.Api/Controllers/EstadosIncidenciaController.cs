using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.EstadosIncidencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints del catálogo de estados de incidencia; las escrituras exigen Configuracion.Incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
[ApiController]
[Route("api/estados-incidencia")]
[Produces("application/json")]
public sealed class EstadosIncidenciaController(IEstadoIncidenciaService servicio) : ControllerBase
{
    /// <summary>Lista estados por orden y nombre.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EstadoIncidenciaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EstadoIncidenciaDto>>> Listar([FromQuery] bool soloActivos,
        CancellationToken ct) =>
        Ok(await servicio.ListarAsync(soloActivos, ct));

    /// <summary>Obtiene un estado por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EstadoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoIncidenciaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un estado activo.</summary>
    [HttpPost]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<EstadoIncidenciaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoIncidenciaDto>> Crear([FromBody] EstadoIncidenciaRequest request,
        CancellationToken ct)
    {
        var estado = await servicio.CrearAsync(request, ct);
        return Created($"/api/estados-incidencia/{estado.Id}", estado);
    }

    /// <summary>Edita nombre, descripción y orden de un estado.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<EstadoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoIncidenciaDto>> Editar(int id, [FromBody] EstadoIncidenciaRequest request,
        CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));

    /// <summary>Desactiva el estado (idempotente); 409 si es inicial de algún flujo.</summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<EstadoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoIncidenciaDto>> Desactivar(int id, CancellationToken ct) =>
        Ok(await servicio.DesactivarAsync(id, ct));

    /// <summary>Activa el estado (idempotente).</summary>
    [HttpPost("{id:int}/activar")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<EstadoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoIncidenciaDto>> Activar(int id, CancellationToken ct) =>
        Ok(await servicio.ActivarAsync(id, ct));
}

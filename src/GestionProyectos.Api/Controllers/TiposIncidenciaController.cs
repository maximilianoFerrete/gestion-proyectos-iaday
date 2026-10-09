using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.TiposIncidencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de tipos de incidencia; las escrituras exigen Configuracion.Incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
[ApiController]
[Route("api/tipos-incidencia")]
[Produces("application/json")]
public sealed class TiposIncidenciaController(ITipoIncidenciaService servicio) : ControllerBase
{
    /// <summary>Lista tipos por número de nivel y nombre.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TipoIncidenciaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TipoIncidenciaDto>>> Listar([FromQuery] bool soloActivos,
        [FromQuery] int? nivelId, CancellationToken ct) =>
        Ok(await servicio.ListarAsync(soloActivos, nivelId, ct));

    /// <summary>Obtiene un tipo por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<TipoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoIncidenciaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un tipo activo.</summary>
    [HttpPost]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<TipoIncidenciaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TipoIncidenciaDto>> Crear([FromBody] CrearTipoIncidenciaRequest request,
        CancellationToken ct)
    {
        var tipo = await servicio.CrearAsync(request, ct);
        return Created($"/api/tipos-incidencia/{tipo.Id}", tipo);
    }

    /// <summary>Edita nombre, descripción y flujo de un tipo.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<TipoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TipoIncidenciaDto>> Editar(int id, [FromBody] EditarTipoIncidenciaRequest request,
        CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));

    /// <summary>Desactiva el tipo (idempotente).</summary>
    [HttpPost("{id:int}/desactivar")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<TipoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoIncidenciaDto>> Desactivar(int id, CancellationToken ct) =>
        Ok(await servicio.DesactivarAsync(id, ct));

    /// <summary>Activa el tipo (idempotente).</summary>
    [HttpPost("{id:int}/activar")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<TipoIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TipoIncidenciaDto>> Activar(int id, CancellationToken ct) =>
        Ok(await servicio.ActivarAsync(id, ct));
}

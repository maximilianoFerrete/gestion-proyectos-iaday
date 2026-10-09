using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.NivelesIncidencia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de niveles de incidencia; las escrituras exigen Configuracion.Incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
[ApiController]
[Route("api/niveles-incidencia")]
[Produces("application/json")]
public sealed class NivelesIncidenciaController(INivelIncidenciaService servicio) : ControllerBase
{
    /// <summary>Lista los niveles ordenados por número.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NivelIncidenciaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NivelIncidenciaDto>>> Listar(CancellationToken ct) =>
        Ok(await servicio.ListarAsync(ct));

    /// <summary>Obtiene un nivel por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<NivelIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NivelIncidenciaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un nivel.</summary>
    [HttpPost]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<NivelIncidenciaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NivelIncidenciaDto>> Crear([FromBody] CrearNivelIncidenciaRequest request,
        CancellationToken ct)
    {
        var nivel = await servicio.CrearAsync(request, ct);
        return Created($"/api/niveles-incidencia/{nivel.Id}", nivel);
    }

    /// <summary>Edita nombre y descripción de un nivel.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permisos.ConfiguracionIncidencias)]
    [ProducesResponseType<NivelIncidenciaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NivelIncidenciaDto>> Editar(int id,
        [FromBody] EditarNivelIncidenciaRequest request, CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));
}

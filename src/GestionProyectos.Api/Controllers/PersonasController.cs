using GestionProyectos.Application.Personas;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints del catálogo de personas.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
[ApiController]
[Route("api/personas")]
[Produces("application/json")]
public sealed class PersonasController(IPersonaService servicio) : ControllerBase
{
    /// <summary>Lista personas por texto (nombre o email); por defecto solo activas.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PersonaDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PersonaDto>>> Listar([FromQuery] string? texto,
        [FromQuery] bool incluirInactivas, CancellationToken ct) =>
        Ok(await servicio.ListarAsync(texto, incluirInactivas, ct));

    /// <summary>Obtiene una persona por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea una persona; esAdmin = true exige administrador.</summary>
    [HttpPost]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonaDto>> Crear([FromBody] PersonaRequest request, CancellationToken ct)
    {
        var persona = await servicio.CrearAsync(request, ct);
        return Created($"/api/personas/{persona.Id}", persona);
    }

    /// <summary>Edita una persona; cambiar esAdmin exige administrador.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonaDto>> Editar(int id, [FromBody] PersonaRequest request,
        CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));

    /// <summary>Desactiva una persona (idempotente).</summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonaDto>> Desactivar(int id, CancellationToken ct) =>
        Ok(await servicio.DesactivarAsync(id, ct));

    /// <summary>Activa una persona (idempotente).</summary>
    [HttpPost("{id:int}/activar")]
    [ProducesResponseType<PersonaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaDto>> Activar(int id, CancellationToken ct) =>
        Ok(await servicio.ActivarAsync(id, ct));
}

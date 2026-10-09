using GestionProyectos.Application.EstadosProyecto;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoints de configuración de estados de proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
[ApiController]
[Route("api/estados-proyecto")]
[Produces("application/json")]
public sealed class EstadosProyectoController(IEstadoProyectoService servicio) : ControllerBase
{
    /// <summary>Lista los estados (incluido el de sistema) por orden y nombre.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<EstadoProyectoDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EstadoProyectoDto>>> Listar([FromQuery] bool soloActivos,
        CancellationToken ct) =>
        Ok(await servicio.ListarAsync(soloActivos, ct));

    /// <summary>Obtiene un estado por id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EstadoProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EstadoProyectoDto>> Obtener(int id, CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(id, ct));

    /// <summary>Crea un estado configurable.</summary>
    [HttpPost]
    [ProducesResponseType<EstadoProyectoDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoProyectoDto>> Crear([FromBody] EstadoProyectoRequest request,
        CancellationToken ct)
    {
        var estado = await servicio.CrearAsync(request, ct);
        return Created($"/api/estados-proyecto/{estado.Id}", estado);
    }

    /// <summary>Edita un estado configurable.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<EstadoProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoProyectoDto>> Editar(int id, [FromBody] EstadoProyectoRequest request,
        CancellationToken ct) =>
        Ok(await servicio.EditarAsync(id, request, ct));

    /// <summary>Marca el estado como inicial y desmarca el anterior.</summary>
    [HttpPost("{id:int}/marcar-inicial")]
    [ProducesResponseType<EstadoProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoProyectoDto>> MarcarInicial(int id, CancellationToken ct) =>
        Ok(await servicio.MarcarInicialAsync(id, ct));

    /// <summary>Desactiva el estado (idempotente).</summary>
    [HttpPost("{id:int}/desactivar")]
    [ProducesResponseType<EstadoProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoProyectoDto>> Desactivar(int id, CancellationToken ct) =>
        Ok(await servicio.DesactivarAsync(id, ct));

    /// <summary>Activa el estado (idempotente).</summary>
    [HttpPost("{id:int}/activar")]
    [ProducesResponseType<EstadoProyectoDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EstadoProyectoDto>> Activar(int id, CancellationToken ct) =>
        Ok(await servicio.ActivarAsync(id, ct));
}

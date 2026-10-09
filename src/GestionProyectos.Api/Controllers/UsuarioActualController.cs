using GestionProyectos.Application.UsuarioActual;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Controllers;

/// <summary>
/// Endpoint del usuario actual (spec 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
[ApiController]
[Route("api/usuario-actual")]
[Produces("application/json")]
public sealed class UsuarioActualController(IUsuarioActualService servicio) : ControllerBase
{
    /// <summary>Devuelve el usuario actual o uno anónimo si no se identificó.</summary>
    [HttpGet]
    [ProducesResponseType<UsuarioActualDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UsuarioActualDto>> Obtener(CancellationToken ct) =>
        Ok(await servicio.ObtenerAsync(ct));
}

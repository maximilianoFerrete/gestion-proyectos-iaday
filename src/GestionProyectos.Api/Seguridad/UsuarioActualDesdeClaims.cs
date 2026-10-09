using System.Globalization;
using System.Security.Claims;
using GestionProyectos.Application.Abstracciones;

namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// IUsuarioActual a partir del ClaimsPrincipal; no conoce el header ni el esquema (spec 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class UsuarioActualDesdeClaims(IHttpContextAccessor accesor) : IUsuarioActual
{
    private ClaimsPrincipal? Principal =>
        accesor.HttpContext?.User is { Identity.IsAuthenticated: true } usuario ? usuario : null;

    /// <inheritdoc />
    public bool EstaIdentificado => PersonaId is not null;

    /// <inheritdoc />
    public int? PersonaId =>
        int.TryParse(Principal?.FindFirst(EsquemaProvisional.ClaimSub)?.Value, NumberStyles.None,
            CultureInfo.InvariantCulture, out var id)
            ? id
            : null;

    /// <inheritdoc />
    public string? Nombre => Principal?.FindFirst(EsquemaProvisional.ClaimNombre)?.Value;

    /// <inheritdoc />
    public bool EsAdmin => Principal?.IsInRole(EsquemaProvisional.RolAdmin) ?? false;
}

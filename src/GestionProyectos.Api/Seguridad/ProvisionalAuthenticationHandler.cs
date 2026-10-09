using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GestionProyectos.Application.UsuarioActual;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// Esquema "Provisional": identifica al usuario por el header X-Usuario-Id (spec 0.4, DA-04).
/// Falsificable: solo para desarrollo/demo hasta que exista autenticación real.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class ProvisionalAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IIdentificacionUsuarioService identificacion)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Sin header: anónimo. Header inválido, inexistente o inactivo: Fail (401).</summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(EsquemaProvisional.Header, out var valores))
        {
            return AuthenticateResult.NoResult();
        }

        if (!int.TryParse(valores.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            || id <= 0)
        {
            return AuthenticateResult.Fail($"El header {EsquemaProvisional.Header} debe ser un id numérico.");
        }

        var identidad = await identificacion.BuscarActivaAsync(id, Context.RequestAborted);
        if (identidad is null)
        {
            return AuthenticateResult.Fail($"El usuario {id} no existe o está inactivo.");
        }

        var principal = new ClaimsPrincipal(CrearIdentidad(identidad, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    /// <summary>Construye la identidad con los claims sub, name, email y role.</summary>
    public static ClaimsIdentity CrearIdentidad(IdentidadUsuario identidad, string esquema)
    {
        ArgumentNullException.ThrowIfNull(identidad);
        var claims = new List<Claim>
        {
            new(EsquemaProvisional.ClaimSub, identidad.PersonaId.ToString(CultureInfo.InvariantCulture)),
            new(EsquemaProvisional.ClaimNombre, identidad.Nombre),
            new(EsquemaProvisional.ClaimEmail, identidad.Email),
        };
        if (identidad.EsAdmin)
        {
            claims.Add(new Claim(EsquemaProvisional.ClaimRol, EsquemaProvisional.RolAdmin));
        }

        return new ClaimsIdentity(claims, esquema, EsquemaProvisional.ClaimNombre, EsquemaProvisional.ClaimRol);
    }
}

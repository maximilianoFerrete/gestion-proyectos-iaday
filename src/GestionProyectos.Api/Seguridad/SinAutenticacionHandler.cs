using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// Esquema usado cuando el modo provisional está apagado y aún no hay login real: todo es anónimo.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class SinAutenticacionHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Nunca identifica usuarios.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.NoResult());
}
